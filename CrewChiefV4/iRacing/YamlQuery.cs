using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Windows;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace CrewChiefV4.iRacing
{
    public class YamlQuery
    {
        protected YamlQuery(YamlQuery parent, bool error, YamlMappingNode root, string key)
        {
            _path = parent == null ? "" : parent.QueryPath;
            _path += key + ":";

            this.IsError = error;
            if (!error)
            {
                // Find next mapping node
                this.Node = Find(root, key);
            }
        }

        protected YamlQuery(YamlQuery parent, bool error, YamlSequenceNode root, string key, object value)
        {
            _path = parent == null ? "" : parent.QueryPath;
            _path += string.Format("{0}:{{{1}}}", key, value);

            // Find sequencing node with matching value
            this.IsError = error;
            if (!error)
            {
                // Find next mapping node
                this.Node = FindSequence(root, key, value);
            }
        }

        internal static YamlQuery Mapping(YamlMappingNode root, string key)
        {
            return new YamlQuery(null, false, root, key);
        }

        private static YamlQuery Error(YamlQuery parent, string error, string key)
        {
            var query = new YamlQuery(parent, true, null, key);
            query.ErrorMessage = error;
            return query;
        }

        private static YamlQuery Error(YamlQuery parent, string error, string key, object value)
        {
            var query = new YamlQuery(parent, true, null, key, value);
            query.ErrorMessage = error;
            return query;
        }

        private string _path;
        public string QueryPath { get { return _path; } }

        protected bool IsError { get; private set; }
        internal string ErrorMessage { get; set; }

        internal YamlNode Node { get; set; }

        private YamlNode Find(YamlMappingNode node, string key)
        {
            if (node == null) this.IsError = true;
            if (this.IsError) return null;
            return node.Children.FirstOrDefault(kvp => kvp.Key.ToString() == key).Value;
        }

        private YamlNode FindSequence(YamlSequenceNode node, string key, object value)
        {
            if (this.IsError) return null;

            foreach (var child in node.Children)
            {
                var mapping = (YamlMappingNode) child;
                foreach (var mappingChild in mapping.Children)
                {
                    if (mappingChild.Key.ToString() == key)
                    {
                        // Found key, check value, otherwise skip this mappingChild
                        if (mappingChild.Value.ToString() == value.ToString())
                        {
                            // Value matches, return mapping
                            return mapping;
                        }
                        else
                        {
                            // Value does not match, skip this child
                            break;
                        }
                    }
                }
            }
            return null;
        }

        private string GetValueSafe()
        {
            if (this.IsError) return null;
            if (this.Node == null) return null;

            var type = this.Node.GetType();
            if (type == typeof(YamlScalarNode))
            {
                var scalar = (YamlScalarNode)this.Node;
                return scalar.Value;
            }
            return null;
        }

        /// <summary>
        /// Gets the value of the result of the current YamlQuery. May throw an exception if the query is invalid or does not return data.
        /// Use GetValue method to prevent exceptions.
        /// </summary>
        public string Value
        {
            get
            {
                if (this.IsError)
                {
                    throw new YamlQueryException(this.ErrorMessage);
                }

                var value = this.GetValueSafe();
                if (value == null)
                    throw new YamlQueryException("The YAML path returned null: " + this.QueryPath);

                return value; 
            }
        }

        /// <summary>
        /// Gets the value of the result of the current YamlQuery, or returns a default value (or null) in case the query is invalid or does not return data.
        /// </summary>
        /// <param name="defaultValue">The default value to return in case the query is invalid or does not return data.</param>
        public string GetValue(string defaultValue = null)
        {
            try
            {
                return this.Value;
            }
            catch (Exception ex)
            {
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Attempts to get the value of the result of the current YamlQuery and modifies the parameter with the value. Returns true if the value was found, 
        /// or false if the query is invalid or does not return data.
        /// </summary>
        /// <param name="val">This parameter is modified and contains the result of the query if valid, or null otherwise.</param>
        public bool TryGetValue(out string val)
        {
            try
            {
                val = this.Value;
                return true;
            }
            catch (Exception)
            {
                val = null;
                return false;
            }
        }

        public YamlQuery this[string key]
        {
            get
            {
                // Are we in an incorrect path?
                if (this.IsError) return YamlQuery.Error(this, this.ErrorMessage, key);

                // Current node should be a mapping node
                if (this.Node == null)
                    return YamlQuery.Error(this, "The YAML query path is incorrect: " + this.QueryPath, key);

                var type = this.Node.GetType();
                if (type != typeof(YamlMappingNode))
                    return YamlQuery.Error(this, 
                        string.Format("The YAML query path '{0}' is incorrect: expected a YamlMappingNode, but received a {1}.", this.QueryPath, type.Name),
                        key);

                return new YamlQuery(this, false, (YamlMappingNode)this.Node, key);
            }
        }

        public YamlQuery this[string key, object value]
        {
            get
            {
                // Are we in an incorrect path?
                if (this.IsError) return YamlQuery.Error(this, this.ErrorMessage, key, value);

                // Current node should be a sequence node
                if (this.Node == null)
                    return YamlQuery.Error(this, "The YAML query path is incorrect: " + this.QueryPath, key, value);

                var type = this.Node.GetType();
                if (type != typeof(YamlSequenceNode))
                    return YamlQuery.Error(this,
                        string.Format("The YAML query path '{0}' is incorrect: expected a YamlSequenceNode, but received a {1}.", this.QueryPath, type.Name), key, value);

                return new YamlQuery(this, false, (YamlSequenceNode)this.Node, key, value);
            }
        }

        public override string ToString()
        {
            return this.QueryPath;
        }

        public class YamlQueryException : Exception
        {
            public YamlQueryException(string message) : base(message) { }
        }
    }
}
