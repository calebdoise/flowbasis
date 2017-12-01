using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.Expressions
{
    public class ExpressionEvaluator
    {
        private ExpressionScope scope;

        public ExpressionEvaluator() : this(null)
        {
        }

        public ExpressionEvaluator(ExpressionScope scope)
        {
            if (scope != null)
            {
                this.scope = scope;
            }
            else
            {
                this.scope = new ExpressionScope();
            }
        }


        public virtual object Evaluate(string expression)
        {
            if (String.IsNullOrWhiteSpace(expression))
            {
                return null;
            }

            var jsepParser = new JsepParser();
            var node = jsepParser.Parse(expression);

            var result = this.Evaluate(node);
            return result;
        }

        public virtual object Evaluate(JsepNode node)
        {
            if (node == null)
            {
                return null;
            }

            switch (node.Type)
            {
                case JsepNodeType.Literal:
                    {
                        return node.Value;
                    }

                case JsepNodeType.BinaryExpression:
                    {                        
                        object result = this.EvaluateBinaryExpression(node);
                        return result;
                    }

                case JsepNodeType.UnaryExpression:
                    {
                        object result = this.EvaluateUnaryExpression(node);
                        return result;
                    }

                case JsepNodeType.ConditionalExpression:
                    {
                        object result = this.EvaluateConditionalExpression(node);
                        return result;
                    }

                case JsepNodeType.LogicalExpression:
                    {
                        object result = this.EvaluateLogicalExpression(node);
                        return result;
                    }

                case JsepNodeType.ArrayExpression:
                    {
                        object result = this.EvaluateArrayExpression(node);
                        return result;
                    }

                case JsepNodeType.Identifier:
                    {                        
                        object result = this.scope.EvaluateIdentifier(node.Name);
                        return result;
                    }            

                case JsepNodeType.Compound:
                    {
                        object result = this.EvaluateCompoundExpression(node);
                        return result;
                    }

                case JsepNodeType.MemberExpression:
                    {
                        object result = this.EvaluateMemberExpression(node);
                        return result;
                    }

                case JsepNodeType.ThisExpression:
                    {
                        object result = this.scope.EvaluateThis();
                        return result;
                    }

                case JsepNodeType.CallExpression:
                    {
                        object result = this.EvaluateCallExpression(node);
                        return result;
                    }

                default:
                    {
                        throw new NotSupportedException($"Unsupported node type: {node.Type}");
                    }
            }           
        }


        protected virtual object EvaluateBinaryExpression(JsepNode node)
        {
            if (node.Type != JsepNodeType.BinaryExpression)
            {
                throw new Exception("Expected BinaryExpression");
            }

            object left = this.Evaluate(node.Left);
            object right = this.Evaluate(node.Right);

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


        protected virtual object EvaluateUnaryExpression(JsepNode node)
        {
            if (node.Type != JsepNodeType.UnaryExpression)
            {
                throw new Exception("Expected UnaryExpression");
            }

            object arg = this.Evaluate(node.Argument);

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


        protected virtual object EvaluateConditionalExpression(JsepNode node)
        {
            if (node.Type != JsepNodeType.ConditionalExpression)
            {
                throw new Exception("Expected UnaryExpression");
            }

            object testResult = this.Evaluate(node.Test);
            if (this.IsConsideredTrue(testResult))
            {
                object result = this.Evaluate(node.Consequent);
                return result;
            }
            else
            {
                object result = this.Evaluate(node.Alternate);
                return result;
            }
        }

        protected virtual object EvaluateLogicalExpression(JsepNode node)
        {
            if (node.Type != JsepNodeType.LogicalExpression)
            {
                throw new Exception("Expected LogicalExpression");
            }
            
            object left = this.Evaluate(node.Left);

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
                    object right = this.Evaluate(node.Right);
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
                    object right = this.Evaluate(node.Right);
                    bool isTrue = this.IsConsideredTrue(right);
                    return isTrue;
                }
            }
            else
            {
                throw new NotSupportedException($"Unsupported operator: {node.Operator}");
            }            
        }


        protected virtual object EvaluateArrayExpression(JsepNode node)
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
                    object element = this.Evaluate(elementNode);
                    list.Add(element);
                }
                return list;
            }
            else
            {
                return null;
            }
        }


        protected virtual object EvaluateCompoundExpression(JsepNode node)
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
                    lastResult = this.Evaluate(elementNode);                    
                }
                return lastResult;
            }
            else
            {
                return null;
            }
        }


        protected virtual object EvaluateMemberExpression(JsepNode node)
        {
            if (node.Type != JsepNodeType.MemberExpression)
            {
                throw new Exception("Expected MemberExpression");
            }

            var instance = this.Evaluate(node.Object);

            string memberName = null;
            if (node.Property?.Type == JsepNodeType.Identifier)
            {
                memberName = node.Property.Name;
            }
            else
            {
                memberName = this.Evaluate(node.Property) as string;
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
            else
            {
                throw new Exception("Object instance cannot provide member information.");
            }            
        }


        protected virtual object EvaluateCallExpression(JsepNode node)
        {
            if (node.Type != JsepNodeType.CallExpression)
            {
                throw new Exception("Expected CallExpression");
            }

            var callee = this.Evaluate(node.Callee);
            if (callee is IExpressionCallable callable)
            {
                object[] args = new object[node.Arguments?.Count ?? 0];

                if (node.Arguments != null)
                {
                    for (int co = 0; co < node.Arguments.Count; co++)
                    {
                        var argValue = this.Evaluate(node.Arguments[co]);
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
    }
}
