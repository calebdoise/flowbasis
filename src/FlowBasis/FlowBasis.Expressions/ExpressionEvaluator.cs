using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.Expressions
{
    public class ExpressionEvaluator
    {
        private ExpressionScope defaultScope;

        public ExpressionEvaluator() : this(null)
        {
        }

        public ExpressionEvaluator(ExpressionScope defaultScope)
        {
            if (defaultScope != null)
            {
                this.defaultScope = defaultScope;
            }
            else
            {
                this.defaultScope = new ExpressionScope();
            }
        }


        public virtual object Evaluate(string expression, ExpressionScope scopeToUse = null)
        {
            if (String.IsNullOrWhiteSpace(expression))
            {
                return null;
            }

            var jsepParser = new JsepParser();
            var node = jsepParser.Parse(expression);

            // Wrap scope to use with a nested expression evaluator that provides a temporary variable holder.
            if (scopeToUse == null)
            {
                scopeToUse = this.defaultScope;
            }
            var nestedScopeToUse = new NestedExpressionScopeWithTempVariable(scopeToUse);

            var result = this.Evaluate(node, nestedScopeToUse);
            return result;
        }

        public virtual object Evaluate(JsepNode node, ExpressionScope scopeToUse = null)
        {
            return this.InternalEvaluate(node, scopeToUse);
        }

        public virtual object InternalEvaluate(JsepNode node, ExpressionScope scopeToUse)
        {
            if (node == null)
            {
                return null;
            }

            if (scopeToUse == null)
            {
                scopeToUse = this.defaultScope;
            }

            switch (node.Type)
            {
                case JsepNodeType.Literal:
                    {
                        return node.Value;
                    }

                case JsepNodeType.BinaryExpression:
                    {                        
                        object result = this.EvaluateBinaryExpression(node, scopeToUse);
                        return result;
                    }

                case JsepNodeType.UnaryExpression:
                    {
                        object result = this.EvaluateUnaryExpression(node, scopeToUse);
                        return result;
                    }

                case JsepNodeType.ConditionalExpression:
                    {
                        object result = this.EvaluateConditionalExpression(node, scopeToUse);
                        return result;
                    }

                case JsepNodeType.LogicalExpression:
                    {
                        object result = this.EvaluateLogicalExpression(node, scopeToUse);
                        return result;
                    }

                case JsepNodeType.ArrayExpression:
                    {
                        object result = this.EvaluateArrayExpression(node, scopeToUse);
                        return result;
                    }

                case JsepNodeType.Identifier:
                    {                        
                        object result = scopeToUse.EvaluateIdentifier(node.Name);
                        return result;
                    }            

                case JsepNodeType.Compound:
                    {
                        object result = this.EvaluateCompoundExpression(node, scopeToUse);
                        return result;
                    }

                case JsepNodeType.MemberExpression:
                    {
                        object result = this.EvaluateMemberExpression(node, scopeToUse);
                        return result;
                    }

                case JsepNodeType.ThisExpression:
                    {
                        object result = scopeToUse.EvaluateThis();
                        return result;
                    }

                case JsepNodeType.CallExpression:
                    {
                        object result = this.EvaluateCallExpression(node, scopeToUse);
                        return result;
                    }

                default:
                    {
                        throw new NotSupportedException($"Unsupported node type: {node.Type}");
                    }
            }           
        }


        protected virtual object EvaluateBinaryExpression(JsepNode node, ExpressionScope scopeToUse)
        {
            if (node.Type != JsepNodeType.BinaryExpression)
            {
                throw new Exception("Expected BinaryExpression");
            }

            object left = this.InternalEvaluate(node.Left, scopeToUse);
            object right = this.InternalEvaluate(node.Right, scopeToUse);

            bool treatBothAsIntegers = false;
            bool treatBothAsDecimals = false;
            if (this.IsIntegerType(left))
            {
                if (this.IsIntegerType(right))
                {
                    treatBothAsIntegers = true;
                    treatBothAsDecimals = false;
                }
                else if (this.IsDecimalType(right))
                {
                    treatBothAsIntegers = false;
                    treatBothAsDecimals = true;
                }
                else
                {
                    treatBothAsIntegers = false;
                    treatBothAsDecimals = false;
                }
            }
            else if (this.IsDecimalType(left))
            {
                treatBothAsIntegers = false;
                treatBothAsDecimals = true;
            }


            switch (node.Operator)
            {
                case "+":
                    {
                        if (treatBothAsIntegers)
                        {
                            long result = Convert.ToInt64(left) + Convert.ToInt64(right);
                            return result;
                        }
                        else if (treatBothAsDecimals)
                        {                       
                            decimal result = Convert.ToDecimal(left) + Convert.ToDecimal(right);
                            return result;
                        }
                        else if (left is string leftStr)
                        {
                            string concatStr = leftStr + right?.ToString();
                            return concatStr;
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported operands: {node.Operator}");
                        }
                    }

                case "*":
                    {
                        if (treatBothAsIntegers)
                        {
                            long result = Convert.ToInt64(left) * Convert.ToInt64(right);
                            return result;
                        }
                        else if (treatBothAsDecimals)
                        {
                            decimal result = Convert.ToDecimal(left) * Convert.ToDecimal(right);
                            return result;
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported operands: {node.Operator}");
                        }
                    }

                case "-":
                    {
                        if (treatBothAsIntegers)
                        {
                            long result = Convert.ToInt64(left) - Convert.ToInt64(right);
                            return result;
                        }
                        else if (treatBothAsDecimals)
                        {
                            decimal result = Convert.ToDecimal(left) - Convert.ToDecimal(right);
                            return result;
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported operands: {node.Operator}");
                        }
                    }

                case "/":
                    {
                        if (treatBothAsDecimals || treatBothAsIntegers)
                        {
                            decimal result = Convert.ToDecimal(left) / Convert.ToDecimal(right);
                            return result;
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported operands: {node.Operator}");
                        }
                    }

                case "<":
                    {
                        if (treatBothAsDecimals || treatBothAsIntegers)
                        {
                            bool result = Convert.ToDecimal(left) < Convert.ToDecimal(right);
                            return result;
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported operands: {node.Operator}");
                        }
                    }

                case "<=":
                    {
                        if (treatBothAsDecimals || treatBothAsIntegers)
                        {
                            bool result = Convert.ToDecimal(left) <= Convert.ToDecimal(right);
                            return result;
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported operands: {node.Operator}");
                        }
                    }

                case ">":
                    {
                        if (treatBothAsDecimals || treatBothAsIntegers)
                        {
                            bool result = Convert.ToDecimal(left) > Convert.ToDecimal(right);
                            return result;
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported operands: {node.Operator}");
                        }
                    }

                case ">=":
                    {
                        if (treatBothAsDecimals || treatBothAsIntegers)
                        {
                            bool result = Convert.ToDecimal(left) >= Convert.ToDecimal(right);
                            return result;
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported operands: {node.Operator}");
                        }
                    }

                case "==":
                    {
                        return Object.Equals(left, right);
                    }

                case "!=":
                    {
                        return !Object.Equals(left, right);
                    }

                default:
                    {
                        throw new NotSupportedException($"Unsupported operator: {node.Operator}");
                    }
            }
        }


        protected virtual object EvaluateUnaryExpression(JsepNode node, ExpressionScope scopeToUse)
        {
            if (node.Type != JsepNodeType.UnaryExpression)
            {
                throw new Exception("Expected UnaryExpression");
            }

            object arg = this.InternalEvaluate(node.Argument, scopeToUse);

            switch (node.Operator)
            {
                case "!":
                    {
                        return this.IsConsideredTrue(arg) ? false : true;  
                    }

                case "-":
                    {
                        if (this.IsDecimalType(arg))
                        {
                            return -Convert.ToDecimal(arg);
                        }
                        else if (this.IsIntegerType(arg))
                        {
                            return -Convert.ToInt64(arg);
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported operands: {node.Operator}");
                        }
                    }

                default:
                    {
                        throw new NotSupportedException($"Unsupported operator: {node.Operator}");
                    }
            }
        }


        protected virtual object EvaluateConditionalExpression(JsepNode node, ExpressionScope scopeToUse)
        {
            if (node.Type != JsepNodeType.ConditionalExpression)
            {
                throw new Exception("Expected UnaryExpression");
            }

            object testResult = this.InternalEvaluate(node.Test, scopeToUse);
            if (this.IsConsideredTrue(testResult))
            {
                object result = this.InternalEvaluate(node.Consequent, scopeToUse);
                return result;
            }
            else
            {
                object result = this.InternalEvaluate(node.Alternate, scopeToUse);
                return result;
            }
        }

        protected virtual object EvaluateLogicalExpression(JsepNode node, ExpressionScope scopeToUse)
        {
            if (node.Type != JsepNodeType.LogicalExpression)
            {
                throw new Exception("Expected LogicalExpression");
            }
            
            object left = this.InternalEvaluate(node.Left, scopeToUse);

            if (node.Operator == "&&")
            {
                // AND 
                if (!this.IsConsideredTrue(left))
                {
                    // short circuit the remaining test.
                    return false;
                }
                else
                {
                    object right = this.InternalEvaluate(node.Right, scopeToUse);
                    bool isTrue = this.IsConsideredTrue(right);
                    return isTrue;
                }
            }
            else if (node.Operator == "||")
            {
                // OR
                if (this.IsConsideredTrue(left))
                {
                    // short circuit the remaining test.
                    return true;
                }
                else
                {
                    object right = this.InternalEvaluate(node.Right, scopeToUse);
                    bool isTrue = this.IsConsideredTrue(right);
                    return isTrue;
                }
            }
            else
            {
                throw new NotSupportedException($"Unsupported operator: {node.Operator}");
            }            
        }


        protected virtual object EvaluateArrayExpression(JsepNode node, ExpressionScope scopeToUse)
        {
            if (node.Type != JsepNodeType.ArrayExpression)
            {
                throw new Exception("Expected ArrayExpression");
            }

            if (node.Elements != null)
            {
                var list = new List<object>(node.Elements.Count);
                foreach (var elementNode in node.Elements)
                {
                    object element = this.InternalEvaluate(elementNode, scopeToUse);
                    list.Add(element);
                }
                return list;
            }
            else
            {
                return null;
            }
        }


        protected virtual object EvaluateCompoundExpression(JsepNode node, ExpressionScope scopeToUse)
        {
            if (node.Type != JsepNodeType.Compound)
            {
                throw new Exception("Expected Compound");
            }

            if (node.Body != null)
            {
                // TODO: Provide optional mode where each compound result is returned as array.
                object lastResult = null;
                foreach (var elementNode in node.Body)
                {
                    lastResult = this.InternalEvaluate(elementNode, scopeToUse);                    
                }
                return lastResult;
            }
            else
            {
                return null;
            }
        }


        protected virtual object EvaluateMemberExpression(JsepNode node, ExpressionScope scopeToUse)
        {
            if (node.Type != JsepNodeType.MemberExpression)
            {
                throw new Exception("Expected MemberExpression");
            }

            var instance = this.InternalEvaluate(node.Object, scopeToUse);

            string memberName = null;
            if (node.Property?.Type == JsepNodeType.Identifier)
            {
                memberName = node.Property.Name;
            }
            else
            {
                memberName = this.InternalEvaluate(node.Property, scopeToUse) as string;
            }

            if (memberName == null)
            {
                throw new Exception("Member specified must be a fixed identifier or resolve to a string.");
            }

            if (instance is IExpressionMemberProvider memberProvider)
            {               
                var memberValue = memberProvider.EvaluateMember(memberName);
                return memberValue;
            }
            else if (instance is FlowBasis.Json.JObject instanceAsJObject)
            {
                var memberValue = instanceAsJObject[memberName];
                return memberValue;
            }
            else if (instance is string str)
            {
                var memberValue = this.EvaluateStringMember(str, memberName);
                return memberValue;
            }
            else
            {
                throw new Exception("Object instance cannot provide member information.");
            }            
        }


        protected virtual object EvaluateCallExpression(JsepNode node, ExpressionScope scopeToUse)
        {
            if (node.Type != JsepNodeType.CallExpression)
            {
                throw new Exception("Expected CallExpression");
            }

            var callee = this.InternalEvaluate(node.Callee, scopeToUse);
            if (callee is IExpressionCallable callable)
            {
                object[] args = new object[node.Arguments?.Count ?? 0];

                if (node.Arguments != null)
                {
                    for (int co = 0; co < node.Arguments.Count; co++)
                    {
                        var argValue = this.InternalEvaluate(node.Arguments[co], scopeToUse);
                        args[co] = argValue;
                    }
                }

                object result = callable.EvaluateCall(args);
                return result;
            }
            else
            {
                throw new Exception("Target expression is not callable.");
            }
        }


        protected virtual bool IsConsideredTrue(object value)
        {
            if (value == null)
            {
                return false;
            }

            if (value is bool b)
            {
                return b;
            }
            else if (value is decimal d)
            {                
                return d != 0;
            }            
            else if (value is string s)
            {
                return s.Length != 0;
            }
            
            return false;
        }


        private bool IsIntegerType(object value)
        {
            if (value is int || value is long || value is short || value is byte)
            {
                return true;
            }

            return false;
        }

        private bool IsDecimalType(object value)
        {
            if (value is decimal || value is float || value is double)
            {
                return true;
            }

            return false;
        }

        private bool IsNumericType(object value)
        {
            return this.IsIntegerType(value) || this.IsDecimalType(value);
        }


        protected virtual object EvaluateStringMember(string str, string memberName)
        {
            switch(memberName)
            {
                case "length": return str?.Length;

                case "indexOf": return new StringIndexOfExpressionCallable(str, lastIndex: false);
                case "lastIndexOf": return new StringIndexOfExpressionCallable(str, lastIndex: true);
                case "substring": return new StringSubstringExpressionCallable(str);

                // Return portion of string after the requested string.
                case "after": return new StringAfterExpressionCallable(str, afterLast: false);
                case "afterLast": return new StringAfterExpressionCallable(str, afterLast: true);
            }

            throw new Exception($"Member not found on string: {memberName}");
        }



        private class StringIndexOfExpressionCallable : IExpressionCallable
        {
            private string str;
            private bool lastIndex;
            
            public StringIndexOfExpressionCallable(string str, bool lastIndex)
            {
                this.str = str;
                this.lastIndex = lastIndex;
            }

            public object EvaluateCall(object[] args)
            {
                if (args.Length == 1)
                {
                    string find = args[0]?.ToString();
                    if (find != null)
                    {
                        int index = this.lastIndex ? this.str.LastIndexOf(find) : this.str.IndexOf(find);
                        return index;
                    }
                }

                return null;
            }
        }


        private class StringAfterExpressionCallable : IExpressionCallable
        {
            private string str;
            private bool afterLast;

            public StringAfterExpressionCallable(string str, bool afterLast)
            {
                this.str = str;
                this.afterLast = afterLast;
            }

            public object EvaluateCall(object[] args)
            {
                if (args.Length == 1)
                {
                    string find = args[0]?.ToString();
                    if (find != null)
                    {
                        int index = afterLast ? this.str.LastIndexOf(find) : this.str.IndexOf(find);
                        if (index == -1)
                        {
                            return null;
                        }
                        else
                        {
                            string strAfter = this.str.Substring(index + find.Length);
                            return strAfter;
                        }                       
                    }
                }

                return null;
            }
        }


        private class StringSubstringExpressionCallable : IExpressionCallable
        {
            private string str;

            public StringSubstringExpressionCallable(string str)
            {
                this.str = str;
            }

            public object EvaluateCall(object[] args)
            {
                if (args.Length == 1)
                {
                    int index = Convert.ToInt32(args[0]);
                    string substring = this.str.Substring(index);
                    return substring;
                }
                else if (args.Length == 2)
                {
                    int index = Convert.ToInt32(args[0]);
                    int length = Convert.ToInt32(args[1]);

                    if (index >= this.str.Length)
                    {
                        return String.Empty;
                    }
                    
                    int finalLength = Math.Min(length, this.str.Length - index);

                    string substring = this.str.Substring(index, finalLength);
                    return substring;
                }

                return null;
            }
        }
    }
}
