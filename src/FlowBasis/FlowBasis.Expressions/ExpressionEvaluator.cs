using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.Expressions
{
    public class ExpressionEvaluator
    {

        public ExpressionEvaluator()
        {
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
                        throw new NotSupportedException($"Unsupported node type: {node.Type}");
                    }                

                case JsepNodeType.Compound:
                    {
                        throw new NotSupportedException($"Unsupported node type: {node.Type}");
                    }

                case JsepNodeType.MemberExpression:
                    {
                        throw new NotSupportedException($"Unsupported node type: {node.Type}");
                    }

                case JsepNodeType.ThisExpression:
                    {
                        throw new NotSupportedException($"Unsupported node type: {node.Type}");
                    }

                case JsepNodeType.CallExpression:
                    {
                        throw new NotSupportedException($"Unsupported node type: {node.Type}");
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

            switch (node.Operator)
            {
                case "+":
                    {
                        if (left is decimal leftDecimal)
                        {                       
                            decimal sum = leftDecimal + Convert.ToDecimal(right);
                            return sum;
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
                        if (left is decimal leftDecimal)
                        {
                            decimal product = leftDecimal * Convert.ToDecimal(right);
                            return product;
                        }                       
                        else
                        {
                            throw new NotSupportedException($"Unsupported operands: {node.Operator}");
                        }
                    }

                case "-":
                    {
                        if (left is decimal leftDecimal)
                        {
                            decimal delta = leftDecimal - Convert.ToDecimal(right);
                            return delta;
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported operands: {node.Operator}");
                        }
                    }

                case "/":
                    {
                        if (left is decimal leftDecimal)
                        {
                            decimal fraction = leftDecimal / Convert.ToDecimal(right);
                            return fraction;
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported operands: {node.Operator}");
                        }
                    }

                case "<":
                    {
                        if (left is decimal leftDecimal)
                        {
                            return leftDecimal < Convert.ToDecimal(right);
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported operands: {node.Operator}");
                        }
                    }

                case "<=":
                    {
                        if (left is decimal leftDecimal)
                        {
                            return leftDecimal <= Convert.ToDecimal(right);
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported operands: {node.Operator}");
                        }
                    }

                case ">":
                    {
                        if (left is decimal leftDecimal)
                        {
                            return leftDecimal > Convert.ToDecimal(right);
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported operands: {node.Operator}");
                        }
                    }

                case ">=":
                    {
                        if (left is decimal leftDecimal)
                        {
                            return leftDecimal >= Convert.ToDecimal(right);
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
                        if (arg is decimal argDecimal)
                        {
                            return -argDecimal;
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
    }
}
