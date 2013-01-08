
namespace Telerik.DynamicProxy
{
    ///<summary>
    /// Defines the user constructor for the proxy.
    ///</summary>
    public class Ctor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Ctor"/> class.
        /// </summary>
        public Ctor(bool proxied)
        {
            this.proxied = proxied;
        }

        /// <summary>
        /// Gets a value indicating if the constructor is to be proxied.
        /// </summary>
        internal bool Proxied
        {
            get { return proxied; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ctor"/> class.
        /// </summary>
        /// <param name="args"></param>
        public Ctor(params object[] args)
        {
            this.args = args;
        }

        internal object[] ToArgArray()
        {
            if (args == null)
                return new object[0];
            return args;
        }

        private readonly bool proxied;
        private readonly object[] args;
    }
}
