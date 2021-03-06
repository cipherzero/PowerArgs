﻿using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Linq;
namespace PowerArgs
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ArgReviverAttribute : Attribute
    {
        public ArgReviverAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ArgActionType : Attribute
    {
        public Type ActionType { get; private set; }
        public ArgActionType(Type t)
        {
            this.ActionType = t;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ArgStyleAttribute : Attribute
    {
        public ArgStyle Style { get; set; }
        public ArgStyleAttribute(ArgStyle style = ArgStyle.PowerShell)
        {
            this.Style = style;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class ArgIgnoreCase : Attribute
    {
        public bool IgnoreCase { get; set; }

        public ArgIgnoreCase(bool ignore = true)
        {
            IgnoreCase = ignore;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ArgShortcut : Attribute
    {
        public string Shortcut { get; set; }

        public ArgShortcut(string shortcut)
        {
            this.Shortcut = shortcut;
        }

        public static string GetShortcut(PropertyInfo info)
        {
            var actionProperty = ArgAction.GetActionProperty(info.DeclaringType);
            if (actionProperty != null && actionProperty.Name == info.Name) return null;

            var attr = info.Attr<ArgShortcut>();

            if (attr == null) return info.GetArgumentName()[0] + "";
            else return attr.Shortcut;
        }
    }

    #region Usage

    [AttributeUsage(AttributeTargets.Property)]
    public class ArgIgnoreAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class ArgPosition : Attribute
    {
        public int Position { get; private set; }
        public ArgPosition(int pos)
        {
            this.Position = pos;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ArgDescription : Attribute
    {
        public string Description { get; private set; }
        public ArgDescription(string description)
        {
            this.Description = description;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
    public class ArgExample : Attribute
    {
        public string Example { get; private set; }
        public string Description { get; private set; }
        public ArgExample(string example, string description)
        {
            this.Example = example;
            this.Description = description;
        }
    } 

    #endregion

    #region Hooks

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public abstract class ArgHook : Attribute
    {
        public class HookContext
        {
            public PropertyInfo Property { get; set; }
            public string[] CmdLineArgs;
            public string ArgumentValue;
            public object Args { get; set; }
            public object RevivedProperty;
            public ArgParser Parser { get; set; }
        }

        // Higher goes first
        public int BeforePopulatePropertyPriority { get; set; }
        public int AfterPopulatePropertyPriority { get; set; }
        public int BeforePopulatePropertiesPriority { get; set; }
        public int AfterPopulatePropertiesPriority { get; set; }
        public int BeforeParsePriority { get; set; }

        public virtual void BeforeParse(HookContext context) { }
        public virtual void BeforePopulateProperty(HookContext context) { }
        public virtual void AfterPopulateProperty(HookContext context) { }
        public virtual void BeforePopulateProperties(HookContext context) { }
        public virtual void AfterPopulateProperties(HookContext context) { }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class StickyArg : ArgHook
    {
        private string file;
        private Dictionary<string, string> stickyArgs { get; set; }

        public StickyArg() : this(null) { }

        public StickyArg(string file)
        {
            stickyArgs = new Dictionary<string, string>();
            this.file = file ?? Assembly.GetEntryAssembly().Location + ".StickyArgs.txt";
            Load();
            BeforePopulatePropertyPriority = 10;
        }

        public override void BeforePopulateProperty(HookContext Context)
        {
            if (Context.ArgumentValue == null) Context.ArgumentValue = GetStickyArg(Context.Property.GetArgumentName());
        }

        public override void AfterPopulateProperty(HookContext Context)
        {
            if (Context.ArgumentValue != null) SetStickyArg(Context.Property.GetArgumentName(), Context.ArgumentValue);
        }

        public string GetStickyArg(string name)
        {
            string ret = null;
            if (stickyArgs.TryGetValue(name, out ret) == false) return null;
            return ret;
        }

        public void SetStickyArg(string name, string value)
        {
            if (stickyArgs.ContainsKey(name))
            {
                stickyArgs[name] = value;
            }
            else
            {
                stickyArgs.Add(name, value);
            }
            Save();
        }

        private void Load()
        {
            stickyArgs.Clear();

            if (File.Exists(file) == false) return;

            foreach (var line in File.ReadAllLines(file))
            {
                int separator = line.IndexOf("=");
                if (separator < 0 || line.Trim().StartsWith("#")) continue;

                string key = line.Substring(0, separator).Trim();
                string val = separator == line.Length - 1 ? "" : line.Substring(separator + 1).Trim();

                stickyArgs.Add(key, val);
            }
        }

        private void Save()
        {
            var lines = (from k in stickyArgs.Keys select k + "=" + stickyArgs[k]).ToArray();
            File.WriteAllLines(file, lines);
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultValueAttribute : ArgHook
    {
        public object Value { get; private set; }
        public DefaultValueAttribute(object value)
        {
            Value = value;
        }

        public override void BeforePopulateProperty(HookContext Context)
        {
            if (Context.ArgumentValue == null) Context.ArgumentValue = Value.ToString();
        }
    }

    #endregion
}
