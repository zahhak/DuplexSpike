using System;

namespace Telerik.DynamicProxy
{
    /// <summary>
    /// Wrapper for invocation arguments.
    /// </summary>
    public class Argument
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Argument"/> class.
        /// </summary>
        /// <param name="value">Defines the value of the argument</param>
        /// <param name="type">Defines the type of the argument</param>
        /// <param name="isOut">Defines if the argument has an output modifier</param>
        public Argument(object value, Type type, bool isOut)
        {
            Value = value;
            this.type = type;
            this.isOut = isOut;
        }

        /// <summary>
        /// Gets the argument value/ Set it for output argument.
        /// </summary>
        public object Value { get; set; }


        /// <summary>
        /// Gets type of the argument.
        /// </summary>
        public Type Type 
        {
            get
            {
                return type;
            }
        }

        /// <summary>
        /// Gets <value>true</value> for output param.
        /// </summary>
        public bool IsOut
        {
            get
            {
                return isOut;
            }
        }

        private readonly Type type;
        private readonly bool isOut;
    }
}
