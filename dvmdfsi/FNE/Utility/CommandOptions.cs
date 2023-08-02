﻿/**
* Digital Voice Modem - Fixed Network Equipment
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment
*
*/
/*
*   Copyright (C) 2022 by Bryan Biedenkapp N2PLL
*
*   This program is free software: you can redistribute it and/or modify
*   it under the terms of the GNU Affero General Public License as published by
*   the Free Software Foundation, either version 3 of the License, or
*   (at your option) any later version.
*
*   This program is distributed in the hope that it will be useful,
*   but WITHOUT ANY WARRANTY; without even the implied warranty of
*   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*   GNU Affero General Public License for more details.
*/
//
// Implements a class to help manage command line options and flags.
// Created Aug 30, 2012
//
//
// Options.cs
//
// Authors:
//  Jonathan Pryor <jpryor@novell.com>
//  Federico Di Gregorio <fog@initd.org>
//
// Copyright (C) 2008 Novell (http://www.novell.com)
// Copyright (C) 2009 Federico Di Gregorio.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

// Compile With:
//   gmcs -debug+ -r:System.Core Options.cs -o:NDesk.Options.dll
//   gmcs -debug+ -d:LINQ -r:System.Core Options.cs -o:NDesk.Options.dll
//
// The LINQ version just changes the implementation of
// OptionSet.Parse(IEnumerable<string>), and confers no semantic changes.

//
// A Getopt::Long-inspired option parsing library for C#.
//
// NDesk.Options.OptionSet is built upon a key/value table, where the
// key is a option format string and the value is a delegate that is
// invoked when the format string is matched.
//
// Option format strings:
//  Regex-like BNF Grammar:
//    name: .+
//    type: [=:]
//    sep: ( [^{}]+ | '{' .+ '}' )?
//    aliases: ( name type sep ) ( '|' name type sep )*
//
// Each '|'-delimited name is an alias for the associated action.  If the
// format string ends in a '=', it has a required value.  If the format
// string ends in a ':', it has an optional value.  If neither '=' or ':'
// is present, no value is supported.  `=' or `:' need only be defined on one
// alias, but if they are provided on more than one they must be consistent.
//
// Each alias portion may also end with a "key/value separator", which is used
// to split option values if the option accepts > 1 value.  If not specified,
// it defaults to '=' and ':'.  If specified, it can be any character except
// '{' and '}' OR the *string* between '{' and '}'.  If no separator should be
// used (i.e. the separate values should be distinct arguments), then "{}"
// should be used as the separator.
//
// Options are extracted either from the current option by looking for
// the option name followed by an '=' or ':', or is taken from the
// following option IFF:
//  - The current option does not contain a '=' or a ':'
//  - The current option requires a value (i.e. not a Option type of ':')
//
// The `name' used in the option format string does NOT include any leading
// option indicator, such as '-', '--', or '/'.  All three of these are
// permitted/required on any named option.
//
// Option bundling is permitted so long as:
//   - '-' is used to start the option group
//   - all of the bundled options are a single character
//   - at most one of the bundled options accepts a value, and the value
//     provided starts from the next character to the end of the string.
//
// This allows specifying '-a -b -c' as '-abc', and specifying '-D name=value'
// as '-Dname=value'.
//
// Option processing is disabled by specifying "--".  All options after "--"
// are returned by OptionSet.Parse() unchanged and unprocessed.
//
// Unprocessed options are returned from OptionSet.Parse().
//
// Examples:
//  int verbose = 0;
//  OptionSet p = new OptionSet ()
//    .Add ("v", v => ++verbose)
//    .Add ("name=|value=", v => Console.WriteLine (v));
//  p.Parse (new string[]{"-v", "--v", "/v", "-name=A", "/name", "B", "extra"});
//
// The above would parse the argument string array, and would invoke the
// lambda expression three times, setting `verbose' to 3 when complete.
// It would also print out "A" and "B" to standard output.
// The returned array would contain the string "extra".
//
// C# 3.0 collection initializers are supported and encouraged:
//  var p = new OptionSet () {
//    { "h|?|help", v => ShowHelp () },
//  };
//
// System.ComponentModel.TypeConverter is also supported, allowing the use of
// custom data types in the callback type; TypeConverter.ConvertFromString()
// is used to convert the value option to an instance of the specified
// type:
//
//  var p = new OptionSet () {
//    { "foo=", (Foo f) => Console.WriteLine (f.ToString ()) },
//  };
//
// Random other tidbits:
//  - Boolean options (those w/o '=' or ':' in the option format string)
//    are explicitly enabled if they are followed with '+', and explicitly
//    disabled if they are followed with '-':
//      string a = null;
//      var p = new OptionSet () {
//        { "a", s => a = s },
//      };
//      p.Parse (new string[]{"-a"});   // sets v != null
//      p.Parse (new string[]{"-a+"});  // sets v != null
//      p.Parse (new string[]{"-a-"});  // sets v == null
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;

#if LINQ
using System.Linq;
#endif

namespace dvmdfsi.FNE.Utility
{
    /// <summary>
    /// </summary>
    public static class StringCoda
    {
        /*
        ** Methods
        */

        /// <summary>
        /// </summary>
        public static IEnumerable<string> WrappedLines(string self, params int[] widths)
        {
            IEnumerable<int> w = widths;
            return WrappedLines(self, w);
        }

        /// <summary>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="widths"></param>
        /// <returns></returns>
        public static IEnumerable<string> WrappedLines(string self, IEnumerable<int> widths)
        {
            if (widths == null)
                throw new ArgumentNullException("widths");
            return CreateWrappedLinesIterator(self, widths);
        }

        /// <summary>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="widths"></param>
        /// <returns></returns>
        private static IEnumerable<string> CreateWrappedLinesIterator(string self, IEnumerable<int> widths)
        {
            if (string.IsNullOrEmpty(self))
            {
                yield return string.Empty;
                yield break;
            }

            using (IEnumerator<int> ewidths = widths.GetEnumerator())
            {
                bool? hw = null;
                int width = GetNextWidth(ewidths, int.MaxValue, ref hw);
                int start = 0, end;
                do
                {
                    end = GetLineEnd(start, width, self);
                    char c = self[end - 1];
                    if (char.IsWhiteSpace(c))
                        --end;
                    bool needContinuation = end != self.Length && !IsEolChar(c);
                    string continuation = "";
                    if (needContinuation)
                    {
                        --end;
                        continuation = "-";
                    }

                    string line = self.Substring(start, end - start) + continuation;
                    yield return line;
                    start = end;
                    if (char.IsWhiteSpace(c))
                        ++start;
                    width = GetNextWidth(ewidths, width, ref hw);
                } while (start < self.Length);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="ewidths"></param>
        /// <param name="curWidth"></param>
        /// <param name="eValid"></param>
        /// <returns></returns>
        private static int GetNextWidth(IEnumerator<int> ewidths, int curWidth, ref bool? eValid)
        {
            if (!eValid.HasValue || eValid.HasValue && eValid.Value)
            {
                curWidth = (eValid = ewidths.MoveNext()).Value ? ewidths.Current : curWidth;

                // '.' is any character, - is for a continuation
                const string minWidth = ".-";
                if (curWidth < minWidth.Length)
                {
                    throw new ArgumentOutOfRangeException("widths",
                        string.Format("Element must be >= {0}, was {1}.", minWidth.Length, curWidth));
                }

                return curWidth;
            }

            // no more elements, use the last element.
            return curWidth;
        }

        /// <summary>
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static bool IsEolChar(char c)
        {
            return !char.IsLetterOrDigit(c);
        }

        /// <summary>
        /// </summary>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        private static int GetLineEnd(int start, int length, string description)
        {
            int end = Math.Min(start + length, description.Length);
            int sep = -1;
            for (int i = start; i < end; ++i)
            {
                if (description[i] == '\n')
                    return i + 1;
                if (IsEolChar(description[i]))
                    sep = i + 1;
            }

            if (sep == -1 || end == description.Length)
                return end;
            return sep;
        }
    } // internal static class StringCoda

    /// <summary>
    /// </summary>
    public class OptionValueCollection : IList, IList<string>
    {
        private readonly OptionContext c;
        private readonly List<string> values = new List<string>();

        /*
        ** Properties
        */

        /// <summary>
        /// </summary>
        public int Count
        {
            get { return values.Count; }
        }

        /// <summary>
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        object IList.this[int index]
        {
            get { return this[index]; }
            set { (values as IList)[index] = value; }
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string this[int index]
        {
            get
            {
                AssertValid(index);
                return index >= values.Count ? null : values[index];
            }
            set { values[index] = value; }
        }

        /// <summary>
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get { return (values as ICollection).IsSynchronized; }
        }

        /// <summary>
        /// </summary>
        object ICollection.SyncRoot
        {
            get { return (values as ICollection).SyncRoot; }
        }

        /// <summary>
        /// </summary>
        bool IList.IsFixedSize
        {
            get { return false; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionValueCollection" /> class.
        /// </summary>
        /// <param name="c"></param>
        internal OptionValueCollection(OptionContext c)
        {
            this.c = c;
        }

        /// <summary>
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        void ICollection.CopyTo(Array array, int index)
        {
            (values as ICollection).CopyTo(array, index);
        }

        /// <summary>
        /// </summary>
        public void Clear()
        {
            values.Clear();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return values.GetEnumerator();
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        int IList.Add(object value)
        {
            return (values as IList).Add(value);
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool IList.Contains(object value)
        {
            return (values as IList).Contains(value);
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        int IList.IndexOf(object value)
        {
            return (values as IList).IndexOf(value);
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        void IList.Insert(int index, object value)
        {
            (values as IList).Insert(index, value);
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        void IList.Remove(object value)
        {
            (values as IList).Remove(value);
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        void IList.RemoveAt(int index)
        {
            (values as IList).RemoveAt(index);
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        public void Add(string item)
        {
            values.Add(item);
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(string item)
        {
            return values.Contains(item);
        }

        /// <summary>
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(string[] array, int arrayIndex)
        {
            values.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(string item)
        {
            return values.Remove(item);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public IEnumerator<string> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(string item)
        {
            return values.IndexOf(item);
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, string item)
        {
            values.Insert(index, item);
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            values.RemoveAt(index);
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        private void AssertValid(int index)
        {
            if (c.Option == null)
                throw new InvalidOperationException("OptionContext.Option is null.");
            if (index >= c.Option.MaxValueCount)
                throw new ArgumentOutOfRangeException("index");
            if (c.Option.OptionValueType == OptionValueType.Required && index >= values.Count)
                throw new OptionException(string.Format(c.OptionSet.MessageLocalizer("Missing required value for option '{0}'."), c.OptionName), c.OptionName);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public List<string> ToList()
        {
            return new List<string>(values);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public string[] ToArray()
        {
            return values.ToArray();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Join(", ", values.ToArray());
        }
    } // internal class OptionValueCollection : IList, IList<string>

    /// <summary>
    /// </summary>
    public class OptionContext
    {
        /*
        ** Properties
        */

        /// <summary>
        /// </summary>
        public Option Option { get; set; }

        /// <summary>
        /// </summary>
        public string OptionName { get; set; }

        /// <summary>
        /// </summary>
        public int OptionIndex { get; set; }

        /// <summary>
        /// </summary>
        public OptionSet OptionSet { get; }

        /// <summary>
        /// </summary>
        public OptionValueCollection OptionValues { get; }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionContext" /> class.
        /// </summary>
        /// <param name="set"></param>
        public OptionContext(OptionSet set)
        {
            OptionSet = set;
            OptionValues = new OptionValueCollection(this);
        }
    } // internal class OptionContext

    /// <summary>
    /// Enumeration of the various command line option types.
    /// </summary>
    public enum OptionValueType
    {
        None,
        Optional,
        Required
    }

    /// <summary>
    /// </summary>
    public abstract class Option
    {
        private static readonly char[] NameTerminator = { '=', ':' };

        /*
        ** Properties
        */

        /// <summary>
        /// </summary>
        public string Prototype { get; }

        /// <summary>
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// </summary>
        public OptionValueType OptionValueType { get; }

        /// <summary>
        /// </summary>
        public int MaxValueCount { get; }

        /// <summary>
        /// </summary>
        internal string[] Names { get; }

        /// <summary>
        /// </summary>
        internal string[] ValueSeparators { get; private set; }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="Option" /> class.
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="description"></param>
        protected Option(string prototype, string description) : this(prototype, description, 1)
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Option" /> class.
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="description"></param>
        /// <param name="maxValueCount"></param>
        protected Option(string prototype, string description, int maxValueCount)
        {
            if (prototype == null)
                throw new ArgumentNullException("prototype");
            if (prototype.Length == 0)
                throw new ArgumentException("Cannot be the empty string.", "prototype");
            if (maxValueCount < 0)
                throw new ArgumentOutOfRangeException("maxValueCount");

            Prototype = prototype;
            Description = description;
            MaxValueCount = maxValueCount;
            Names = this is OptionSet.Category

                // append GetHashCode() so that "duplicate" categories have distinct
                // names, e.g. adding multiple "" categories should be valid.
                ?
                new[] { prototype + GetHashCode() } :
                prototype.Split('|');

            if (this is OptionSet.Category)
                return;

            OptionValueType = ParsePrototype();

            if (MaxValueCount == 0 && OptionValueType != OptionValueType.None)
                throw new ArgumentException("Cannot provide maxValueCount of 0 for OptionValueType.Required or " + "OptionValueType.Optional.",
                    "maxValueCount");

            if (OptionValueType == OptionValueType.None && maxValueCount > 1)
                throw new ArgumentException(string.Format("Cannot provide maxValueCount of {0} for OptionValueType.None.", maxValueCount),
                    "maxValueCount");

            if (Array.IndexOf(Names, "<>") >= 0 && (Names.Length == 1 && OptionValueType != OptionValueType.None || Names.Length > 1 && MaxValueCount > 1))
                throw new ArgumentException("The default option handler '<>' cannot require values.", "prototype");
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public string[] GetNames()
        {
            return (string[])Names.Clone();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public string[] GetValueSeparators()
        {
            if (ValueSeparators == null)
                return new string[0];
            return (string[])ValueSeparators.Clone();
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        protected static T Parse<T>(string value, OptionContext c)
        {
            Type tt = typeof(T);
            bool nullable = tt.IsValueType && tt.IsGenericType && !tt.IsGenericTypeDefinition && tt.GetGenericTypeDefinition() == typeof(Nullable<>);
            Type targetType = nullable ? tt.GetGenericArguments()[0] : typeof(T);
            TypeConverter conv = TypeDescriptor.GetConverter(targetType);
            T t = default(T);
            try
            {
                if (value != null)
                    t = (T)conv.ConvertFromString(value);
            }
            catch (Exception e)
            {
                throw new OptionException(string.Format(c.OptionSet.MessageLocalizer("Could not convert string `{0}' to type {1} for option `{2}'."),
                        value, targetType.Name, c.OptionName), c.OptionName, e);
            }

            return t;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private OptionValueType ParsePrototype()
        {
            char type = '\0';
            List<string> seps = new List<string>();
            for (int i = 0; i < Names.Length; ++i)
            {
                string name = Names[i];
                if (name.Length == 0)
                    throw new ArgumentException("Empty option names are not supported.", "prototype");

                int end = name.IndexOfAny(NameTerminator);
                if (end == -1)
                    continue;
                Names[i] = name.Substring(0, end);
                if (type == '\0' || type == name[end])
                    type = name[end];
                else
                    throw new ArgumentException(string.Format("Conflicting option types: '{0}' vs. '{1}'.", type, name[end]), "prototype");

                AddSeparators(name, end, seps);
            }

            if (type == '\0')
                return OptionValueType.None;

            if (MaxValueCount <= 1 && seps.Count != 0)
                throw new ArgumentException(string.Format("Cannot provide key/value separators for Options taking {0} value(s).",
                        MaxValueCount), "prototype");

            if (MaxValueCount > 1)
            {
                if (seps.Count == 0)
                    ValueSeparators = new[] { ":", "=" };
                else if (seps.Count == 1 && seps[0].Length == 0)
                    ValueSeparators = null;
                else
                    ValueSeparators = seps.ToArray();
            }

            return type == '=' ? OptionValueType.Required : OptionValueType.Optional;
        }

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="end"></param>
        /// <param name="seps"></param>
        private static void AddSeparators(string name, int end, ICollection<string> seps)
        {
            int start = -1;
            for (int i = end + 1; i < name.Length; ++i)
            {
                switch (name[i])
                {
                    case '{':
                        if (start != -1)
                            throw new ArgumentException(string.Format("Ill-formed name/value separator found in \"{0}\".", name), "prototype");

                        start = i + 1;
                        break;

                    case '}':
                        if (start == -1)
                            throw new ArgumentException(string.Format("Ill-formed name/value separator found in \"{0}\".", name), "prototype");

                        seps.Add(name.Substring(start, i - start));
                        start = -1;
                        break;

                    default:
                        if (start == -1)
                            seps.Add(name[i].ToString());
                        break;
                }
            }

            if (start != -1)
                throw new ArgumentException(string.Format("Ill-formed name/value separator found in \"{0}\".", name), "prototype");
        }

        /// <summary>
        /// </summary>
        /// <param name="c"></param>
        public void Invoke(OptionContext c)
        {
            OnParseComplete(c);
            c.OptionName = null;
            c.Option = null;
            c.OptionValues.Clear();
        }

        /// <summary>
        /// </summary>
        /// <param name="c"></param>
        protected abstract void OnParseComplete(OptionContext c);

        /// <inheritdoc />
        public override string ToString()
        {
            return Prototype;
        }
    } // internal abstract class Option

    /// <summary>
    /// </summary>
    public abstract class ArgumentSource
    {
        /*
        ** Properties
        */

        /// <summary>
        /// </summary>
        public abstract string Description { get; }

        /*
        ** Methods
        */

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public abstract string[] GetNames();

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="replacement"></param>
        /// <returns></returns>
        public abstract bool GetArguments(string value, out IEnumerable<string> replacement);

        /// <summary>
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetArgumentsFromFile(string file)
        {
            return GetArguments(File.OpenText(file), true);
        }

        /// <summary>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetArguments(TextReader reader)
        {
            return GetArguments(reader, false);
        }

        /// <summary>
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="close"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetArguments(TextReader reader, bool close)
        {
            try
            {
                StringBuilder arg = new StringBuilder();

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    int t = line.Length;

                    for (int i = 0; i < t; i++)
                    {
                        char c = line[i];

                        if (c == '"' || c == '\'')
                        {
                            char end = c;

                            for (i++; i < t; i++)
                            {
                                c = line[i];

                                if (c == end)
                                    break;
                                arg.Append(c);
                            }
                        }
                        else if (c == ' ')
                        {
                            if (arg.Length > 0)
                            {
                                yield return arg.ToString();
                                arg.Length = 0;
                            }
                        }
                        else
                            arg.Append(c);
                    }

                    if (arg.Length > 0)
                    {
                        yield return arg.ToString();
                        arg.Length = 0;
                    }
                }
            }
            finally
            {
                if (close)
                    reader.Close();
            }
        }
    } // internal abstract class ArgumentSource

    /// <summary>
    /// </summary>
    internal class ResponseFileSource : ArgumentSource
    {
        /*
        ** Properties
        */

        /// <inheritdoc />
        public override string Description
        {
            get { return "Read response file for more options."; }
        }

        /*
        ** Methods
        */

        /// <inheritdoc />
        public override string[] GetNames()
        {
            return new[] { "@file" };
        }

        /// <inheritdoc />
        public override bool GetArguments(string value, out IEnumerable<string> replacement)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith("@"))
            {
                replacement = null;
                return false;
            }

            replacement = GetArgumentsFromFile(value.Substring(1));
            return true;
        }
    } // internal class ResponseFileSource : ArgumentSource

    /// <summary>
    /// </summary>
    [Serializable]
    public class OptionException : Exception
    {
        /*
        ** Properties
        */

        /// <summary>
        /// </summary>
        public string OptionName { get; }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionException" /> class.
        /// </summary>
        public OptionException()
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionException" /> class.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="optionName"></param>
        public OptionException(string message, string optionName) : base(message)
        {
            OptionName = optionName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionException" /> class.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="optionName"></param>
        /// <param name="innerException"></param>
        public OptionException(string message, string optionName, Exception innerException) : base(message,
            innerException)
        {
            OptionName = optionName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionException" /> class.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected OptionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            OptionName = info.GetString("OptionName");
        }

        /// <inheritdoc />
        [SecurityPermission(SecurityAction.LinkDemand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("OptionName", OptionName);
        }
    } // internal class OptionException : Exception

    public delegate void OptionAction<TKey, TValue>(TKey key, TValue value);

    /// <summary>
    /// </summary>
    public class OptionSet : KeyedCollection<string, Option>
    {
        private const int OptionWidth = 29;
        private const int Description_FirstWidth = 80 - OptionWidth;
        private const int Description_RemWidth = 80 - OptionWidth - 2;

        private readonly List<ArgumentSource> sources = new List<ArgumentSource>();

        private readonly Regex ValueOption = new Regex(@"^(?<flag>--|-|/)(?<name>[^:=]+)((?<sep>[:=])(?<value>.*))?$");

        /*
        ** Classes
        */

        /// <summary>
        /// </summary>
        internal sealed class Category : Option
        {
            /**
             * Methods
             */

            /// <summary>
            /// Initializes a new instance of the <see cref="Category" /> class.
            /// </summary>
            /// <remarks>
            /// Prototype starts with '=' because this is an invalid prototype
            /// (see Option.ParsePrototype(), and thus it'll prevent Category
            /// instances from being accidentally used as normal options.
            /// </remarks>
            /// <param name="description"></param>
            public Category(string description) : base("=:Category:= " + description, description)
            {
                /* stub */
            }

            /// <inheritdoc />
            protected override void OnParseComplete(OptionContext c)
            {
                throw new NotSupportedException("Category.OnParseComplete should not be invoked.");
            }
        } // internal sealed class Category : Option

        /// <summary>
        /// </summary>
        private sealed class ActionOption : Option
        {
            private readonly Action<OptionValueCollection> action;

            /**
             * Methods
             */

            /// <summary>
            /// Initializes a new instance of the <see cref="ActionOption" /> class.
            /// </summary>
            /// <param name="prototype"></param>
            /// <param name="description"></param>
            /// <param name="count"></param>
            /// <param name="action"></param>
            public ActionOption(string prototype, string description, int count, Action<OptionValueCollection> action) :
                base(prototype, description, count)
            {
                if (action == null)
                    throw new ArgumentNullException("action");
                this.action = action;
            }

            /// <inheritdoc />
            protected override void OnParseComplete(OptionContext c)
            {
                action(c.OptionValues);
            }
        } // private sealed class ActionOption : Option

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private sealed class ActionOption<T> : Option
        {
            private readonly Action<T> action;

            /**
             * Methods
             */

            /// <summary>
            /// Initializes a new instance of the <see cref="ActionOption{T}" /> class.
            /// </summary>
            /// <param name="prototype"></param>
            /// <param name="description"></param>
            /// <param name="action"></param>
            public ActionOption(string prototype, string description, Action<T> action) : base(prototype, description, 1)
            {
                if (action == null)
                    throw new ArgumentNullException("action");
                this.action = action;
            }

            /// <inheritdoc />
            protected override void OnParseComplete(OptionContext c)
            {
                action(Parse<T>(c.OptionValues[0], c));
            }
        } // private sealed class ActionOption<T> : Option

        /// <summary>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        private sealed class ActionOption<TKey, TValue> : Option
        {
            private readonly OptionAction<TKey, TValue> action;

            /// <summary>
            /// Initializes a new instance of the <see cref="ActionOption{TKey, TValue}" /> class.
            /// </summary>
            /// <param name="prototype"></param>
            /// <param name="description"></param>
            /// <param name="action"></param>
            public ActionOption(string prototype, string description, OptionAction<TKey, TValue> action) : base(prototype, description, 2)
            {
                if (action == null)
                    throw new ArgumentNullException("action");
                this.action = action;
            }

            /// <inheritdoc />
            protected override void OnParseComplete(OptionContext c)
            {
                action(Parse<TKey>(c.OptionValues[0], c), Parse<TValue>(c.OptionValues[1], c));
            }
        } // private sealed class ActionOption<TKey, TValue> : Option

        /// <summary>
        /// </summary>
        private class ArgumentEnumerator : IEnumerable<string>
        {
            private readonly List<IEnumerator<string>> sources = new List<IEnumerator<string>>();

            /**
             * Methods
             */

            /// <summary>
            /// Initializes a new instance of the <see cref="ArgumentEnumerator" /> class.
            /// </summary>
            /// <param name="arguments"></param>
            public ArgumentEnumerator(IEnumerable<string> arguments)
            {
                sources.Add(arguments.GetEnumerator());
            }

            /// <summary>
            /// </summary>
            /// <returns></returns>
            public IEnumerator<string> GetEnumerator()
            {
                do
                {
                    IEnumerator<string> c = sources[sources.Count - 1];
                    if (c.MoveNext())
                        yield return c.Current;
                    else
                    {
                        c.Dispose();
                        sources.RemoveAt(sources.Count - 1);
                    }
                } while (sources.Count > 0);
            }

            /// <summary>
            /// </summary>
            /// <returns></returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            /// <summary>
            /// </summary>
            /// <param name="arguments"></param>
            public void Add(IEnumerable<string> arguments)
            {
                sources.Add(arguments.GetEnumerator());
            }
        } // private class ArgumentEnumerator : IEnumerable<string>

        /*
        ** Properties
        */

        /// <summary>
        /// </summary>
        public Converter<string, string> MessageLocalizer { get; }

        /// <summary>
        /// </summary>
        public ReadOnlyCollection<ArgumentSource> ArgumentSources { get; }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionSet" /> class.
        /// </summary>
        public OptionSet() : this(delegate (string f) { return f; })
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionSet" /> class.
        /// </summary>
        /// <param name="localizer"></param>
        public OptionSet(Converter<string, string> localizer)
        {
            MessageLocalizer = localizer;
            ArgumentSources = new ReadOnlyCollection<ArgumentSource>(sources);
        }

        /// <inheritdoc />
        protected override string GetKeyForItem(Option item)
        {
            if (item == null)
                throw new ArgumentNullException("option");
            if (item.Names != null && item.Names.Length > 0)
                return item.Names[0];

            // This should never happen, as it's invalid for Option to be
            // constructed w/o any names.
            throw new InvalidOperationException("Option has no names!");
        }

        /// <summary>
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        [Obsolete("Use KeyedCollection.this[string]")]
        protected Option GetOptionForName(string option)
        {
            if (option == null)
                throw new ArgumentNullException("option");
            try
            {
                return base[option];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        /// <inheritdoc />
        protected override void InsertItem(int index, Option item)
        {
            base.InsertItem(index, item);
            AddImpl(item);
        }

        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            Option p = Items[index];
            base.RemoveItem(index);

            // KeyedCollection.RemoveItem() handles the 0th item
            for (int i = 1; i < p.Names.Length; ++i)
                Dictionary.Remove(p.Names[i]);
        }

        /// <inheritdoc />
        protected override void SetItem(int index, Option item)
        {
            base.SetItem(index, item);
            AddImpl(item);
        }

        /// <summary>
        /// </summary>
        /// <param name="option"></param>
        private void AddImpl(Option option)
        {
            if (option == null)
                throw new ArgumentNullException("option");
            List<string> added = new List<string>(option.Names.Length);
            try
            {
                // KeyedCollection.InsertItem/SetItem handle the 0th name.
                for (int i = 1; i < option.Names.Length; ++i)
                {
                    Dictionary.Add(option.Names[i], option);
                    added.Add(option.Names[i]);
                }
            }
            catch (Exception)
            {
                foreach (string name in added)
                    Dictionary.Remove(name);
                throw;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        public OptionSet Add(string header)
        {
            if (header == null)
                throw new ArgumentNullException("header");
            Add(new Category(header));
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public new OptionSet Add(Option option)
        {
            base.Add(option);
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public OptionSet Add(string prototype, Action<string> action)
        {
            return Add(prototype, null, action);
        }

        /// <summary>
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="description"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public OptionSet Add(string prototype, string description, Action<string> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            Option p = new ActionOption(prototype, description, 1, delegate (OptionValueCollection v) { action(v[0]); });
            base.Add(p);
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public OptionSet Add(string prototype, OptionAction<string, string> action)
        {
            return Add(prototype, null, action);
        }

        /// <summary>
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="description"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public OptionSet Add(string prototype, string description, OptionAction<string, string> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            Option p = new ActionOption(prototype, description, 2,
                delegate (OptionValueCollection v) { action(v[0], v[1]); });
            base.Add(p);
            return this;
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="prototype"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public OptionSet Add<T>(string prototype, Action<T> action)
        {
            return Add(prototype, null, action);
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="prototype"></param>
        /// <param name="description"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public OptionSet Add<T>(string prototype, string description, Action<T> action)
        {
            return Add(new ActionOption<T>(prototype, description, action));
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="prototype"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public OptionSet Add<TKey, TValue>(string prototype, OptionAction<TKey, TValue> action)
        {
            return Add(prototype, null, action);
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="prototype"></param>
        /// <param name="description"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public OptionSet Add<TKey, TValue>(string prototype, string description, OptionAction<TKey, TValue> action)
        {
            return Add(new ActionOption<TKey, TValue>(prototype, description, action));
        }

        /// <summary>
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public OptionSet Add(ArgumentSource source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            sources.Add(source);
            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        protected virtual OptionContext CreateOptionContext()
        {
            return new OptionContext(this);
        }

        /// <summary>
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public List<string> Parse(IEnumerable<string> arguments)
        {
            if (arguments == null)
                throw new ArgumentNullException("arguments");
            OptionContext c = CreateOptionContext();
            c.OptionIndex = -1;
            bool process = true;
            List<string> unprocessed = new List<string>();
            Option def = Contains("<>") ? this["<>"] : null;
            ArgumentEnumerator ae = new ArgumentEnumerator(arguments);
            foreach (string argument in ae)
            {
                ++c.OptionIndex;
                if (argument == "--")
                {
                    process = false;
                    continue;
                }

                if (!process)
                {
                    Unprocessed(unprocessed, def, c, argument);
                    continue;
                }

                if (AddSource(ae, argument))
                    continue;
                if (!Parse(argument, c))
                    Unprocessed(unprocessed, def, c, argument);
            }

            if (c.Option != null)
                c.Option.Invoke(c);
            return unprocessed;
        }

        /// <summary>
        /// </summary>
        /// <param name="ae"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        private bool AddSource(ArgumentEnumerator ae, string argument)
        {
            foreach (ArgumentSource source in sources)
            {
                IEnumerable<string> replacement;
                if (!source.GetArguments(argument, out replacement))
                    continue;
                ae.Add(replacement);
                return true;
            }

            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="extra"></param>
        /// <param name="def"></param>
        /// <param name="c"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        private static bool Unprocessed(ICollection<string> extra, Option def, OptionContext c, string argument)
        {
            if (def == null)
            {
                extra.Add(argument);
                return false;
            }

            c.OptionValues.Add(argument);
            c.Option = def;
            c.Option.Invoke(c);
            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="flag"></param>
        /// <param name="name"></param>
        /// <param name="sep"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected bool GetOptionParts(string argument, out string flag, out string name, out string sep,
            out string value)
        {
            if (argument == null)
                throw new ArgumentNullException("argument");

            flag = name = sep = value = null;
            Match m = ValueOption.Match(argument);
            if (!m.Success) return false;
            flag = m.Groups["flag"].Value;
            name = m.Groups["name"].Value;
            if (m.Groups["sep"].Success && m.Groups["value"].Success)
            {
                sep = m.Groups["sep"].Value;
                value = m.Groups["value"].Value;
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        protected virtual bool Parse(string argument, OptionContext c)
        {
            if (c.Option != null)
            {
                ParseValue(argument, c);
                return true;
            }

            string f, n, s, v;
            if (!GetOptionParts(argument, out f, out n, out s, out v))
                return false;

            Option p;
            if (Contains(n))
            {
                p = this[n];
                c.OptionName = f + n;
                c.Option = p;
                switch (p.OptionValueType)
                {
                    case OptionValueType.None:
                        c.OptionValues.Add(n);
                        c.Option.Invoke(c);
                        break;

                    case OptionValueType.Optional:
                    case OptionValueType.Required:
                        ParseValue(v, c);
                        break;
                }

                return true;
            }

            // no match; is it a bool option?
            if (ParseBool(argument, n, c))
                return true;

            // is it a bundled option?
            if (ParseBundledValue(f, string.Concat(n + s + v), c))
                return true;

            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="option"></param>
        /// <param name="c"></param>
        private void ParseValue(string option, OptionContext c)
        {
            if (option != null)
            {
                foreach (string o in c.Option.ValueSeparators != null ?
                    option.Split(c.Option.ValueSeparators, c.Option.MaxValueCount - c.OptionValues.Count, StringSplitOptions.None) : new[] { option })
                    c.OptionValues.Add(o);
            }

            if (c.OptionValues.Count == c.Option.MaxValueCount || c.Option.OptionValueType == OptionValueType.Optional)
                c.Option.Invoke(c);
            else if (c.OptionValues.Count > c.Option.MaxValueCount)
                throw new OptionException(MessageLocalizer(string.Format("Error: Found {0} option values when expecting {1}.", 
                    c.OptionValues.Count, c.Option.MaxValueCount)), c.OptionName);
        }

        /// <summary>
        /// </summary>
        /// <param name="option"></param>
        /// <param name="n"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private bool ParseBool(string option, string n, OptionContext c)
        {
            Option p;
            string rn;
            if (n.Length >= 1 && (n[n.Length - 1] == '+' || n[n.Length - 1] == '-') &&
                Contains(rn = n.Substring(0, n.Length - 1)))
            {
                p = this[rn];
                string v = n[n.Length - 1] == '+' ? option : null;
                c.OptionName = option;
                c.Option = p;
                c.OptionValues.Add(v);
                p.Invoke(c);
                return true;
            }

            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="f"></param>
        /// <param name="n"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private bool ParseBundledValue(string f, string n, OptionContext c)
        {
            if (f != "-")
                return false;
            for (int i = 0; i < n.Length; ++i)
            {
                Option p;
                string opt = f + n[i];
                string rn = n[i].ToString();
                if (!Contains(rn))
                {
                    if (i == 0)
                        return false;
                    throw new OptionException(
                        string.Format(MessageLocalizer("Cannot bundle unregistered option '{0}'."), opt), opt);
                }

                p = this[rn];
                switch (p.OptionValueType)
                {
                    case OptionValueType.None:
                        Invoke(c, opt, n, p);
                        break;

                    case OptionValueType.Optional:
                    case OptionValueType.Required:
                        {
                            string v = n.Substring(i + 1);
                            c.Option = p;
                            c.OptionName = opt;
                            ParseValue(v.Length != 0 ? v : null, c);
                            return true;
                        }
                    default:
                        throw new InvalidOperationException("Unknown OptionValueType: " + p.OptionValueType);
                }
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="c"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="option"></param>
        private static void Invoke(OptionContext c, string name, string value, Option option)
        {
            c.OptionName = name;
            c.Option = option;
            c.OptionValues.Add(value);
            option.Invoke(c);
        }

        /// <summary>
        /// </summary>
        /// <param name="o"></param>
        public void WriteOptionDescriptions(TextWriter o)
        {
            foreach (Option p in this)
            {
                int written = 0;

                Category c = p as Category;
                if (c != null)
                {
                    WriteDescription(o, p.Description, "", 80, 80);
                    continue;
                }

                if (!WriteOptionPrototype(o, p, ref written))
                    continue;

                if (written < OptionWidth)
                    o.Write(new string(' ', OptionWidth - written));
                else
                {
                    o.WriteLine();
                    o.Write(new string(' ', OptionWidth));
                }

                WriteDescription(o, p.Description, new string(' ', OptionWidth + 2), Description_FirstWidth, Description_RemWidth);
            }

            foreach (ArgumentSource s in sources)
            {
                string[] names = s.GetNames();
                if (names == null || names.Length == 0)
                    continue;

                int written = 0;

                Write(o, ref written, "  ");
                Write(o, ref written, names[0]);
                for (int i = 1; i < names.Length; ++i)
                {
                    Write(o, ref written, ", ");
                    Write(o, ref written, names[i]);
                }

                if (written < OptionWidth)
                    o.Write(new string(' ', OptionWidth - written));
                else
                {
                    o.WriteLine();
                    o.Write(new string(' ', OptionWidth));
                }

                WriteDescription(o, s.Description, new string(' ', OptionWidth + 2), Description_FirstWidth, Description_RemWidth);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="o"></param>
        /// <param name="value"></param>
        /// <param name="prefix"></param>
        /// <param name="firstWidth"></param>
        /// <param name="remWidth"></param>
        private void WriteDescription(TextWriter o, string value, string prefix, int firstWidth, int remWidth)
        {
            bool indent = false;
            foreach (string line in GetLines(MessageLocalizer(GetDescription(value)), firstWidth, remWidth))
            {
                if (indent)
                    o.Write(prefix);
                o.WriteLine(line);
                indent = true;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="o"></param>
        /// <param name="p"></param>
        /// <param name="written"></param>
        /// <returns></returns>
        private bool WriteOptionPrototype(TextWriter o, Option p, ref int written)
        {
            string[] names = p.Names;

            int i = GetNextOptionIndex(names, 0);
            if (i == names.Length)
                return false;

            if (names[i].Length == 1)
            {
                Write(o, ref written, "  -");
                Write(o, ref written, names[0]);
            }
            else
            {
                Write(o, ref written, "      --");
                Write(o, ref written, names[0]);
            }

            for (i = GetNextOptionIndex(names, i + 1); i < names.Length; i = GetNextOptionIndex(names, i + 1))
            {
                Write(o, ref written, ", ");
                Write(o, ref written, names[i].Length == 1 ? "-" : "--");
                Write(o, ref written, names[i]);
            }

            if (p.OptionValueType == OptionValueType.Optional || p.OptionValueType == OptionValueType.Required)
            {
                if (p.OptionValueType == OptionValueType.Optional) Write(o, ref written, MessageLocalizer("["));
                Write(o, ref written, MessageLocalizer("=" + GetArgumentName(0, p.MaxValueCount, p.Description)));
                string sep = p.ValueSeparators != null && p.ValueSeparators.Length > 0 ? p.ValueSeparators[0] : " ";
                for (int c = 1; c < p.MaxValueCount; ++c)
                    Write(o, ref written, MessageLocalizer(sep + GetArgumentName(c, p.MaxValueCount, p.Description)));
                if (p.OptionValueType == OptionValueType.Optional) Write(o, ref written, MessageLocalizer("]"));
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="names"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private static int GetNextOptionIndex(string[] names, int i)
        {
            while (i < names.Length && names[i] == "<>") ++i;
            return i;
        }

        /// <summary>
        /// </summary>
        /// <param name="o"></param>
        /// <param name="n"></param>
        /// <param name="s"></param>
        private static void Write(TextWriter o, ref int n, string s)
        {
            n += s.Length;
            o.Write(s);
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="maxIndex"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        private static string GetArgumentName(int index, int maxIndex, string description)
        {
            if (description == null)
                return maxIndex == 1 ? "VALUE" : "VALUE" + (index + 1);
            string[] nameStart;
            if (maxIndex == 1)
                nameStart = new[] { "{0:", "{" };
            else
                nameStart = new[] { "{" + index + ":" };
            for (int i = 0; i < nameStart.Length; ++i)
            {
                int start, j = 0;
                do
                {
                    start = description.IndexOf(nameStart[i], j);
                } while (start >= 0 && j != 0 ? description[j++ - 1] == '{' : false);

                if (start == -1)
                    continue;
                int end = description.IndexOf("}", start);
                if (end == -1)
                    continue;
                return description.Substring(start + nameStart[i].Length, end - start - nameStart[i].Length);
            }

            return maxIndex == 1 ? "VALUE" : "VALUE" + (index + 1);
        }

        /// <summary>
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        private static string GetDescription(string description)
        {
            if (description == null)
                return string.Empty;
            StringBuilder sb = new StringBuilder(description.Length);
            int start = -1;
            for (int i = 0; i < description.Length; ++i)
            {
                switch (description[i])
                {
                    case '{':
                        if (i == start)
                        {
                            sb.Append('{');
                            start = -1;
                        }
                        else if (start < 0) start = i + 1;

                        break;

                    case '}':
                        if (start < 0)
                        {
                            if (i + 1 == description.Length || description[i + 1] != '}')
                                throw new InvalidOperationException("Invalid option description: " + description);
                            ++i;
                            sb.Append("}");
                        }
                        else
                        {
                            sb.Append(description.Substring(start, i - start));
                            start = -1;
                        }

                        break;

                    case ':':
                        if (start < 0)
                            goto default;
                        start = i + 1;
                        break;

                    default:
                        if (start < 0)
                            sb.Append(description[i]);
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// </summary>
        /// <param name="description"></param>
        /// <param name="firstWidth"></param>
        /// <param name="remWidth"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetLines(string description, int firstWidth, int remWidth)
        {
            return StringCoda.WrappedLines(description, firstWidth, remWidth);
        }
    } // internal class OptionSet : KeyedCollection<string, Option>
} // namespace dvmdfsi.FNE.Utility
