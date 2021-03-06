﻿using System;
using System.Collections.Generic;


namespace FlowBasis.Expressions
{
    public class JsepParser
    {
        private HashSet<string> unary_ops = new HashSet<string>();
        private Dictionary<string, int> binary_ops = new Dictionary<string, int>();
        private Dictionary<string, object> literals = new Dictionary<string, object>();

        public JsepParser()
        {
            this.unary_ops.Add("-");
            this.unary_ops.Add("!");
            this.unary_ops.Add("~");
            this.unary_ops.Add("+");

            this.binary_ops["||"] = 1;
            this.binary_ops["&&"] = 2;
            this.binary_ops["|"] = 3;
            this.binary_ops["^"] = 4;
            this.binary_ops["&"] = 5;
            this.binary_ops["=="] = 6;
            this.binary_ops["!="] = 6;
            this.binary_ops["==="] = 6;
            this.binary_ops["!=="] = 6;
            this.binary_ops["<"] = 7;
            this.binary_ops[">"] = 7;
            this.binary_ops["<="] = 7;
            this.binary_ops[">="] = 7;
            this.binary_ops["<<"] = 8;
            this.binary_ops[">>"] = 8;
            this.binary_ops[">>>"] = 8;
            this.binary_ops["??"] = 9;
            this.binary_ops["+"] = 10;
            this.binary_ops["-"] = 10;
            this.binary_ops["*"] = 11;
            this.binary_ops["/"] = 11;
            this.binary_ops["%"] = 11;

            this.literals["true"] = true;
            this.literals["false"] = false;
            this.literals["null"] = null;
        }

        private static void ThrowError(string message, int index)
        {
            throw new Exception(message + " at character " + index);
        }

        public void AddUnaryOp(string opName)
        {
            this.unary_ops.Add(opName);
        }

        public void AddBinaryOp(string opName, int precedence)
        {
            this.binary_ops[opName] = precedence;
        }

        public void AddLiteral(string name, object value)
        {
            this.literals[name] = value;
        }

        public JsepNode Parse(string expr)
        {                        
            int PERIOD_CODE = 46, // '.'
                COMMA_CODE = 44, // ','
                SQUOTE_CODE = 39, // single quote
                DQUOTE_CODE = 34, // double quotes
                OPAREN_CODE = 40, // (
                CPAREN_CODE = 41, // )
                OBRACK_CODE = 91, // [
                CBRACK_CODE = 93, // ]
                QUMARK_CODE = 63, // ?
                SEMCOL_CODE = 59, // ;
                COLON_CODE = 58; // :

            int getMaxKeyLen(IEnumerable<string> keys)
            {
                int maxLen = 0;
                foreach (string key in keys)
                {
                    maxLen = Math.Max(key.Length, maxLen);
                }

                return maxLen;
            }

            int max_unop_len = getMaxKeyLen(this.unary_ops);
            int max_binop_len = getMaxKeyLen(this.binary_ops.Keys);
     
            string this_str = "this";

            int binaryPrecedence(string op_val)
            {
                if (this.binary_ops.TryGetValue(op_val, out int precedence))
                {
                    return precedence;
                }
                return 0;
            };

            JsepNode createBinaryExpression(string op, JsepNode left, JsepNode right)
            {
                var type = (op == "||" || op == "&&") ? JsepNodeType.LogicalExpression : JsepNodeType.BinaryExpression;
                var node = new JsepNode
                {
                    Type = type,
                    Operator = op,
                    Left = left,
                    Right = right
                };
                return node;
            }

            bool isDecimalDigit(char ch)
            {
                return ((int)ch >= 48 && (int)ch <= 57); // 0...9
            };

            bool isIdentifierStart(char ch)
            {
                return (ch == '$') || (ch == '_') || // `$` and `_`
                        (ch >= 'A' && ch <= 'Z') || // A...Z
                        (ch >= 'a' && ch <= 'z') || // a...z
                        (ch >= 128 && !binary_ops.ContainsKey(ch.ToString())); // any non-ASCII that is not an operator
            };

            bool isIdentifierPart(char ch)
            {
                return (ch == '$') || (ch == '_') || // `$` and `_`
                        (ch >= 'A' && ch <= 'Z') || // A...Z
                        (ch >= 'a' && ch <= 'z') || // a...z
                        (ch >= '0' && ch <= '9') || // 0...9
                        (ch >= 128 && !binary_ops.ContainsKey(ch.ToString())); // any non-ASCII that is not an operator
            };


            // `index` stores the character number we are currently at while `length` is a constant
            // All of the gobbles below will modify `index` as we move along
            int index = 0;
            Func<int, char> exprI = (i) => (i < expr.Length) ? expr[i] : (char)0;
            Func<int, int> exprICode = (i) => (int)exprI(i);
            int length = expr.Length;

            // Push `index` up to the next non-space character
            void gobbleSpaces()
            {
                var ch = exprICode(index);
                // space or tab
                while (ch == 32 || ch == 9 || ch == 10 || ch == 13)
                {
                    ch = exprICode(++index);
                }
            };

            Func<JsepNode> gobbleExpression = null;
            Func<string> gobbleBinaryOp = null;
            Func<JsepNode> gobbleBinaryExpression = null;
            Func<JsepNode> gobbleToken = null;
            Func<JsepNode> gobbleNumericLiteral = null;
            Func<JsepNode> gobbleStringLiteral = null;
            Func<JsepNode> gobbleVariable = null;
            Func<JsepNode> gobbleArray = null;
            Func<JsepNode> gobbleIdentifier = null;
            Func<char, List<JsepNode>> gobbleArguments = null;
            Func<JsepNode> gobbleGroup = null;

            string substr(string str, int startIndex, int count)
            {
                if (str == null || startIndex >= str.Length)
                {
                    return String.Empty;
                }
                return str.Substring(startIndex, Math.Min(count, str.Length - startIndex));
            };

            // The main parsing function. Much of this code is dedicated to ternary expressions
            gobbleExpression = () =>
            {
                var test = gobbleBinaryExpression();
               
                gobbleSpaces();

                if (exprICode(index) == QUMARK_CODE)
                {
                    // Ternary expression: test ? consequent : alternate
                    index++;
                    var consequent = gobbleExpression();
                    if (consequent == null)
                    {
                        ThrowError("Expected expression", index);
                        return null;
                    }
                    gobbleSpaces();
                    if (exprICode(index) == COLON_CODE)
                    {
                        index++;
                        var alternate = gobbleExpression();
                        if (alternate == null)
                        {
                            ThrowError("Expected expression", index);
                        }

                        var exp = new JsepNode
                        {
                            Type = JsepNodeType.ConditionalExpression,
                            Test = test,
                            Consequent = consequent,
                            Alternate = alternate
                        };
                        return exp;
                    }
                    else
                    {
                        ThrowError("Expected :", index);
                        return null;
                    }
                }
                else
                {
                    return test;
                }
            };

            gobbleBinaryOp = () =>
            {
                gobbleSpaces();
                var to_check = substr(expr, index, max_binop_len);                
                var tc_len = to_check.Length;
                while (tc_len > 0)
                {
                    if (binary_ops.ContainsKey(to_check))
                    {
                        index += tc_len;
                        return to_check;
                    }
                    to_check = to_check.Substring(0, --tc_len);
                }

                return null;
            };

            // This function is responsible for gobbling an individual expression,
            // e.g. `1`, `1+2`, `a+(b*2)-Math.sqrt(2)`
            gobbleBinaryExpression = () =>
            {
                JsepNode node;

                // First, try to get the leftmost thing
                // Then, check to see if there's a binary operator operating on that leftmost thing
                JsepNode left = gobbleToken();
                string biop = gobbleBinaryOp() as string;

                // If there wasn't a binary operator, just return the leftmost node
                if (biop == null)
                {
                    return left;
                }

                // Otherwise, we need to start a stack to properly place the binary operations in their
                // precedence structure
                var biop_info = new JsepNode
                {
                    Value = biop,
                    Prec = binaryPrecedence(biop)
                };

                JsepNode right = gobbleToken();
                if (right == null || right == (object)false)
                {
                    ThrowError("Expected expression after " + biop, index);
                }
                var stack = new List<JsepNode>();
                stack.Add(left);
                stack.Add(biop_info);
                stack.Add(right);

                Func<JsepNode> popStack = () =>
                {
                    JsepNode value = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);
                    return value;
                };

                // Properly deal with precedence using [recursive descent](http://www.engr.mun.ca/~theo/Misc/exp_parsing.htm)
                while ((biop = gobbleBinaryOp()) != null)
                {
                    int prec = binaryPrecedence(biop);

                    if (prec == 0)
                    {
                        break;
                    }
                    biop_info = new JsepNode
                    {
                        Value = biop,
                        Prec = prec
                    };

                    // Reduce: make a binary expression from the three topmost entries.
                    while ((stack.Count > 2) && (prec <= (stack[stack.Count - 2].Prec ?? -1)))
                    {
                        right = popStack();
                        biop = popStack().Value as string;
                        left = popStack();
                        node = createBinaryExpression(biop, left, right);
                        stack.Add(node);
                    }

                    node = gobbleToken();
                    if (node == null)
                    {
                        ThrowError("Expected expression after " + biop, index);
                    }
                    stack.Add(biop_info);
                    stack.Add(node);
                }

                int i = stack.Count - 1;
                node = stack[i];
                while (i > 1)
                {
                    node = createBinaryExpression(stack[i - 1].Value as string, stack[i - 2], node);
                    i -= 2;
                }
                return node;
            };


            // An individual part of a binary expression:
            // e.g. `foo.bar(baz)`, `1`, `"abc"`, `(a % 2)` (because it's in parenthesis)
            gobbleToken = () => 
            {
                gobbleSpaces();
                int ch = exprICode(index);

                if (isDecimalDigit((char)ch) || ch == PERIOD_CODE)
                {
                    // Char code 46 is a dot `.` which can start off a numeric literal
                    return gobbleNumericLiteral();
                }
                else if (ch == SQUOTE_CODE || ch == DQUOTE_CODE)
                {
                    // Single or double quotes
                    return gobbleStringLiteral();
                }
                else if (isIdentifierStart((char)ch) || ch == OPAREN_CODE)
                { // open parenthesis
                  // `foo`, `bar.baz`
                    return gobbleVariable();
                }
                else if (ch == OBRACK_CODE)
                {
                    return gobbleArray();
                }
                else
                {
                    string to_check = substr(expr, index, max_unop_len);
                    int tc_len = to_check.Length;
                    while (tc_len > 0)
                    {
                        if (unary_ops.Contains(to_check))
                        {
                            index += tc_len;

                            var exp = new JsepNode
                            {
                                Type = JsepNodeType.UnaryExpression,
                                Operator = to_check,
                                Argument = gobbleToken(),
                                Prefix = true
                            };
                            return exp;                            
                        }
                        to_check = to_check.Substring(0, --tc_len);
                    }

                    return null;
                }
            };

            // Parse simple numeric literals: `12`, `3.4`, `.5`. Do this by using a string to
            // keep track of everything in the numeric literal and then calling `parseFloat` on that string
            gobbleNumericLiteral = () =>
            {
                string number = "";
                while (isDecimalDigit((char)exprICode(index)))
                {
                    number += exprI(index++);
                }

                if (exprICode(index) == PERIOD_CODE)
                { // can start with a decimal marker
                    number += exprI(index++);

                    while (isDecimalDigit((char)exprICode(index)))
                    {
                        number += exprI(index++);
                    }
                }

                char ch = exprI(index);
                if (ch == 'e' || ch == 'E')
                { // exponent marker
                    number += exprI(index++);
                    ch = exprI(index);
                    if (ch == '+' || ch == '-')
                    { // exponent sign
                        number += exprI(index++);
                    }
                    while (isDecimalDigit((char)exprICode(index)))
                    { //exponent itself
                        number += exprI(index++);
                    }
                    if (!isDecimalDigit((char)exprICode(index - 1)))
                    {
                        ThrowError("Expected exponent (" + number + exprI(index) + ")", index);
                    }
                }

                int chCode = exprICode(index);
                // Check to make sure this isn't a variable name that start with a number (123abc)
                if (isIdentifierStart((char)chCode))
                {
                    ThrowError("Variable names cannot start with a number (" +
                                number + exprI(index) + ")", index);
                    return null;
                }
                else if (chCode == PERIOD_CODE)
                {
                    ThrowError("Unexpected period", index);
                    return null;
                }

                var exp = new JsepNode
                {
                    Type = JsepNodeType.Literal,
                    Value = Convert.ToDecimal(number),
                    Raw = number
                };
                return exp;                           
            };

            // Parses a string literal, staring with single or double quotes with basic support for escape codes
            // e.g. `"hello world"`, `'this is\nJSEP'`
            gobbleStringLiteral = () =>
            {
                string str = "";
                char quote = exprI(index++);
                bool closed = false;
                char ch;

                while (index < length)
                {
                    ch = exprI(index++);
                    if (ch == quote)
                    {
                        closed = true;
                        break;
                    }
                    else if (ch == '\\')
                    {
                        // Check for all of the common escape codes
                        ch = exprI(index++);
                        switch (ch)
                        {
                            case 'n': str += '\n'; break;
                            case 'r': str += '\r'; break;
                            case 't': str += '\t'; break;
                            case 'b': str += '\b'; break;
                            case 'f': str += '\f'; break;
                            case 'v': str += '\x0B'; break;
                            default: str += '\\' + ch; break;
                        }
                    }
                    else
                    {
                        str += ch;
                    }
                }

                if (!closed)
                {
                    ThrowError("Unclosed quote after \"" + str + "\"", index);
                }

                var exp = new JsepNode
                {
                    Type = JsepNodeType.Literal,
                    Value = str,
                    Raw = quote + str + quote
                };
                return exp;                
            };

            // Gobbles only identifiers
            // e.g.: `foo`, `_value`, `$x1`
            // Also, this function checks if that identifier is a literal:
            // (e.g. `true`, `false`, `null`) or `this`
            gobbleIdentifier = () =>
            {
                int ch = exprICode(index);
                int start = index;
                string identifier;

                if (isIdentifierStart((char)ch))
                {
                    index++;
                }
                else
                {
                    ThrowError("Unexpected " + exprI(index), index);
                    return null;
                }

                while (index < length)
                {
                    ch = exprICode(index);
                    if (isIdentifierPart((char)ch))
                    {
                        index++;
                    }
                    else
                    {
                        break;
                    }
                }
                identifier = expr.Substring(start, index - start);

                if (literals.ContainsKey(identifier))
                {
                    var exp = new JsepNode
                    {
                        Type = JsepNodeType.Literal,
                        Value = literals[identifier],
                        Raw = identifier
                    };
                    return exp;
                }
                else if (identifier == this_str)
                {
                    var exp = new JsepNode
                    {
                        Type = JsepNodeType.ThisExpression
                    };
                    return exp;                    
                }
                else
                {
                    var exp = new JsepNode
                    {
                        Type = JsepNodeType.Identifier,
                        Name = identifier
                    };
                    return exp;
                }
            };

            // Gobbles a list of arguments within the context of a function call
            // or array literal. This function also assumes that the opening character
            // `(` or `[` has already been gobbled, and gobbles expressions and commas
            // until the terminator character `)` or `]` is encountered.
            // e.g. `foo(bar, baz)`, `my_func()`, or `[bar, baz]`
            gobbleArguments = (termination) =>
            {
                int ch_i;
                var args = new List<JsepNode>();                
                bool closed = false;

                while (index < length)
                {
                    gobbleSpaces();
                    ch_i = exprICode(index);
                    if ((char)ch_i == termination)
                    { // done parsing
                        closed = true;
                        index++;
                        break;
                    }
                    else if (ch_i == COMMA_CODE)
                    { // between expressions
                        index++;
                    }
                    else
                    {
                        var node = gobbleExpression();                        
                        if (node == null || node.Type == JsepNodeType.Compound)
                        {
                            ThrowError("Expected comma", index);
                        }
                        args.Add(node);
                    }
                }
                if (!closed)
                {
                    ThrowError("Expected " + termination, index);
                    return null;
                }
                return args;
            };

            // Gobble a non-literal variable name. This variable name may include properties
            // e.g. `foo`, `bar.baz`, `foo['bar'].baz`
            // It also gobbles function calls:
            // e.g. `Math.acos(obj.angle)`
            gobbleVariable = () =>
            {                
                int ch_i = exprICode(index);
                JsepNode node;

                if (ch_i == OPAREN_CODE)
                {
                    node = gobbleGroup();
                }
                else
                {
                    node = gobbleIdentifier();
                }
                gobbleSpaces();
                ch_i = exprICode(index);
                while (ch_i == PERIOD_CODE || ch_i == OBRACK_CODE || ch_i == OPAREN_CODE)
                {
                    index++;
                    if (ch_i == PERIOD_CODE)
                    {
                        gobbleSpaces();

                        var exp = new JsepNode
                        {
                            Type = JsepNodeType.MemberExpression,
                            Computed = false,
                            Object = node,
                            Property = gobbleIdentifier()
                        };
                        node = exp;
                    }
                    else if (ch_i == OBRACK_CODE)
                    {
                        var exp = new JsepNode
                        {
                            Type = JsepNodeType.MemberExpression,
                            Computed = true,
                            Object = node,
                            Property = gobbleExpression()
                        };
                        node = exp;
                        
                        gobbleSpaces();
                        ch_i = exprICode(index);
                        if (ch_i != CBRACK_CODE)
                        {
                            ThrowError("Unclosed [", index);
                        }
                        index++;
                    }
                    else if (ch_i == OPAREN_CODE)
                    {
                        // A function call is being made; gobble all the arguments
                        var exp = new JsepNode();
                        exp.Type = JsepNodeType.CallExpression;
                        exp.Arguments = gobbleArguments((char)CPAREN_CODE);
                        exp.Callee = node as JsepNode;

                        node = exp;
                    }
                    gobbleSpaces();
                    ch_i = exprICode(index);
                }
                return node;
            };

            // Responsible for parsing a group of things within parentheses `()`
            // This function assumes that it needs to gobble the opening parenthesis
            // and then tries to gobble everything within that parenthesis, assuming
            // that the next thing it should see is the close parenthesis. If not,
            // then the expression probably doesn't have a `)`
            gobbleGroup = () =>
            {
                index++;
                var node = gobbleExpression();
                gobbleSpaces();
                if (exprICode(index) == CPAREN_CODE)
                {
                    index++;
                    return node;
                }
                else
                {
                    ThrowError("Unclosed (", index);
                    return null;
                }
            };

            // Responsible for parsing Array literals `[1, 2, 3]`
            // This function assumes that it needs to gobble the opening bracket
            // and then tries to gobble the expressions as arguments.
            gobbleArray = () =>
            {
                index++;

                var exp = new JsepNode
                {
                    Type = JsepNodeType.ArrayExpression,
                    Elements = gobbleArguments((char)CBRACK_CODE)
                };

                return exp;
            };

            List<JsepNode> nodes = new List<JsepNode>();

            while (index < length)
            {
                int ch_i = exprICode(index);

                // Expressions can be separated by semicolons, commas, or just inferred without any
                // separators
                if (ch_i == SEMCOL_CODE || ch_i == COMMA_CODE)
                {
                    index++; // ignore separators
                }
                else
                {
                    // Try to gobble each expression individually
                    JsepNode node;
                    if ((node = gobbleExpression()) != null)
                    {
                        nodes.Add(node);
                        // If we weren't able to find a binary expression and are out of room, then
                        // the expression passed in probably has too much
                    }
                    else if (index < length)
                    {
                        ThrowError("Unexpected \"" + exprI(index) + "\"", index);
                    }
                }
            }

            // If there's only one expression just try returning the expression
            if (nodes.Count == 1)
            {
                return nodes[0];
            }
            else
            {
                var compoundNode = new JsepNode
                {
                    Type = JsepNodeType.Compound,
                    Body = nodes
                };
                return compoundNode;
            }
        }        

    }
    
}
