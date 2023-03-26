/*
 * Copyright 2023 William Söder
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class FlatSharpCodeModifier
    {
        private static class DeserializationOptions
        {
            public static string Lazy => "FlatBufferDeserializationOption.Lazy";
            public static string Progressive => "FlatBufferDeserializationOption.Progressive";
            public static string Greedy => "FlatBufferDeserializationOption.Greedy";
            public static string GreedyMutable => "FlatBufferDeserializationOption.GreedyMutable";

            public static IList<string> All => new List<string> { Lazy, Progressive, Greedy, GreedyMutable };
            public static IList<string> AllWithoutEnumName => All.Select(o => o.Substring(o.LastIndexOf('.') + 1)).ToList();
        }

        public static string ModifyGeneratedCode(string generatedCode, IDictionary<FileInfo, Schema> schemas)
        {
            foreach (KeyValuePair<FileInfo, Schema> kvp in schemas)
            {
                FileInfo declarationFile = kvp.Key;
                Schema schema = kvp.Value;

                foreach (reflection.Object obj in schema.objects)
                {
                    bool declarationFilePathsAreEqual = IDeclarationFilePropertyExtensions.DeclarationFilePathsAreEqual(
                        obj.declaration_file,
                        Program.ExecutingDirectory.FullName,
                        IDeclarationFilePropertyExtensions.GetDeclarationFileString(declarationFile.FullName, Program.ExecutingDirectory.FullName),
                        Program.ExecutingDirectory.FullName
                    );

                    if (declarationFilePathsAreEqual && obj.IsReferenceType())
                    {
                        generatedCode = ModifyListTypesInReferenceType(generatedCode, schema, obj);
                    }
                }
            }

            return generatedCode;
        }

        private static string ModifyListTypesInReferenceType(string generatedCode, Schema schema, reflection.Object obj)
        {
            ForEachMatch(ref generatedCode, GetTableReaderRegex(obj), matchIndex =>
            {
                obj.ForEachFieldExceptUType(field =>
                {
                    if (field.type.base_type == BaseType.Vector && !field.deprecated)
                    {
                        (int tableReaderStart, int tableReaderEnd) = GetIndexesOfNextBrackets(generatedCode, matchIndex);
                        generatedCode = ReplaceFieldTypesInTableReader(generatedCode, schema, field, tableReaderStart, tableReaderEnd);
                        (tableReaderStart, tableReaderEnd) = GetIndexesOfNextBrackets(generatedCode, matchIndex);

                        if (TableReaderIsLazy(generatedCode, tableReaderStart, tableReaderEnd))
                        {
                            generatedCode = ReplaceReadIndexValueInLazyTableReader(generatedCode, schema, field, tableReaderStart, tableReaderEnd);
                        }
                        else if (TableReaderIsProgressive(generatedCode, tableReaderStart, tableReaderEnd))
                        {
                            generatedCode = ReplaceReadIndexValueInProgressiveTableReader(generatedCode, schema, field, tableReaderStart, tableReaderEnd);
                        }
                        else if (TableReaderIsGreedy(generatedCode, tableReaderStart, tableReaderEnd))
                        {
                            generatedCode = ReplaceReadIndexValueInGreedyTableReader(generatedCode, schema, field, tableReaderStart, tableReaderEnd);
                        }
                        else if (TableReaderIsGreedyMutable(generatedCode, tableReaderStart, tableReaderEnd))
                        {
                            generatedCode = ReplaceReadIndexValueInGreedyTableReader(generatedCode, schema, field, tableReaderStart, tableReaderEnd, true);
                        }
                    }
                });
            });

            Regex classRegex = new Regex(GetClassDefinitionRegex(obj));
            Match classMatch = classRegex.Match(generatedCode);

            if (classMatch.Success)
            {
                obj.ForEachFieldExceptUType(field =>
                {
                    if (field.type.base_type == BaseType.Vector)
                    {
                        (int classStart, int classEnd) = GetIndexesOfNextBrackets(generatedCode, classMatch.Index);
                        generatedCode = ReplaceFieldTypesInClass(generatedCode, schema, field, classStart, classEnd);
                        (classStart, classEnd) = GetIndexesOfNextBrackets(generatedCode, classMatch.Index);
                        generatedCode = ReplaceAssignmentInCopyConstructor(generatedCode, schema, field, classStart, classEnd);
                    }
                });
            }

            return generatedCode;
        }

        private static string ReplaceFieldTypesInTableReader(string generatedCode, Schema schema, Field field, int tableReaderStart, int tableReaderEnd)
        {
            string listType = schema.GetCSharpType(field.type).TrimEnd('?');
            int indexValue = GetIndexValueOfField(generatedCode, schema, field, tableReaderStart, tableReaderEnd, $@"public override System.Collections.Generic.IList<.+>\?* \b{field.name}\b");

            generatedCode = ReplaceAllInRange(
                generatedCode,
                $@"public required override System.Collections.Generic.IList<.+>\?* \b{field.name}\b",
                $"public required override {listType}? {field.name}",
                tableReaderStart,
                tableReaderEnd
            );

            generatedCode = ReplaceAllInRange(
                generatedCode,
                $@"public override System.Collections.Generic.IList<.+>\?* \b{field.name}\b",
                $"public override {listType}? {field.name}",
                tableReaderStart,
                tableReaderEnd
            );

            generatedCode = ReplaceAllInRange(
                generatedCode,
                $@"private System.Collections.Generic.IList<.+>\?* __index{indexValue}Value",
                $"private {listType}? __index{indexValue}Value",
                tableReaderStart,
                tableReaderEnd
            );

            return generatedCode;
        }

        private static string ReplaceFieldTypesInClass(string generatedCode, Schema schema, Field field, int classStart, int classEnd)
        {
            string listType = schema.GetCSharpType(field.type).TrimEnd('?');

            generatedCode = ReplaceAllInRange(
                generatedCode,
                $@"public virtual IList<.+>\?* \b{field.name}\b",
                $"public virtual {listType}? {field.name}",
                classStart,
                classEnd
            );

            return generatedCode;
        }

        private static string ReplaceReadIndexValueInLazyTableReader(string generatedCode, Schema schema, Field field, int tableReaderStart, int tableReaderEnd)
        {
            string listElement = schema.GetCSharpType(field.type.ToElementAsBaseType()).TrimEnd('?');
            string listType = schema.GetCSharpType(field.type).TrimEnd('?');
            int propertyIndex = GetPropertyIndexOfField(generatedCode, schema, field, tableReaderStart, tableReaderEnd);

            if (propertyIndex != -1)
            {
                int indexValue = GetIndexValueOfField(generatedCode, schema, field, tableReaderStart, tableReaderEnd);
                Regex regex = new Regex($@"return ReadIndex{indexValue}Value\(this.__buffer, this.__offset, this.__vtable, this.__remainingDepth\);");
                Match match = regex.Match(generatedCode, propertyIndex);

                if (match.Success)
                {
                    string listCreation = GetListCreation(schema, field, DeserializationOptions.Lazy);
                    generatedCode = regex.Replace(generatedCode, $@"
                        IList<{listElement}>? _index{indexValue}Value = ReadIndex{indexValue}Value(this.__buffer, this.__offset, this.__vtable, this.__remainingDepth);
                        return _index{indexValue}Value != null ? {listCreation}(_index{indexValue}Value) : null;
                    ", 1, propertyIndex);
                }
            }

            return generatedCode;
        }

        private static string ReplaceReadIndexValueInProgressiveTableReader(string generatedCode, Schema schema, Field field, int tableReaderStart, int tableReaderEnd)
        {
            string listElement = schema.GetCSharpType(field.type.ToElementAsBaseType()).TrimEnd('?');
            string listType = schema.GetCSharpType(field.type).TrimEnd('?');
            int propertyIndex = GetPropertyIndexOfField(generatedCode, schema, field, tableReaderStart, tableReaderEnd);

            if (propertyIndex != -1)
            {
                int indexValue = GetIndexValueOfField(generatedCode, schema, field, tableReaderStart, tableReaderEnd);
                Regex regex = new Regex($@"this.__index{indexValue}Value = ReadIndex{indexValue}Value\(this.__buffer, this.__offset, this.__vtable, this.__remainingDepth\);");
                Match match = regex.Match(generatedCode, propertyIndex);

                if (match.Success)
                {
                    string listCreation = GetListCreation(schema, field, DeserializationOptions.Progressive);
                    generatedCode = regex.Replace(generatedCode, $@"
                        IList<{listElement}>? _index{indexValue}Value = ReadIndex{indexValue}Value(this.__buffer, this.__offset, this.__vtable, this.__remainingDepth);
                        this.__index{indexValue}Value = _index{indexValue}Value != null ? {listCreation}(_index{indexValue}Value) : null;
                    ", 1, match.Index);
                }
            }

            return generatedCode;
        }

        private static string ReplaceReadIndexValueInGreedyTableReader(string generatedCode, Schema schema, Field field, int tableReaderStart, int tableReaderEnd, bool greedyMutable = false)
        {
            string listElement = schema.GetCSharpType(field.type.ToElementAsBaseType()).TrimEnd('?');
            string listType = schema.GetCSharpType(field.type).TrimEnd('?');
            int indexValue = GetIndexValueOfField(generatedCode, schema, field, tableReaderStart, tableReaderEnd);
            int initializeMethodIndex = generatedCode.IndexOf("private void Initialize(TInputBuffer buffer, int offset, short remainingDepth)", tableReaderStart, tableReaderEnd - tableReaderStart + 1);

            if (indexValue != -1 && initializeMethodIndex != -1)
            {
                Regex regex = new Regex($@"this.__index{indexValue}Value = ReadIndex{indexValue}Value\(buffer, offset, vtable, remainingDepth\);");
                Match match = regex.Match(generatedCode, initializeMethodIndex);

                if (match.Success)
                {
                    string listCreation = GetListCreation(schema, field, greedyMutable ? DeserializationOptions.GreedyMutable : DeserializationOptions.Greedy);
                    generatedCode = regex.Replace(generatedCode, $@"
                        IList<{listElement}>? _index{indexValue}Value = ReadIndex{indexValue}Value(buffer, offset, vtable, remainingDepth);
                        this.__index{indexValue}Value = _index{indexValue}Value != null ? {listCreation}(_index{indexValue}Value) : null;
                    ", 1, match.Index);
                }
            }

            return generatedCode;
        }

        private static string ReplaceAssignmentInCopyConstructor(string generatedCode, Schema schema, Field field, int classStart, int classEnd)
        {
            string listElement = schema.GetCSharpType(field.type.ToElementAsBaseType()).TrimEnd('?');
            string listType = schema.GetCSharpType(field.type).TrimEnd('?');

            Regex regex = new Regex($@"this.{field.name} = FlatSharp.Compiler.Generated.CloneHelpers_(.+).Clone\(source.{field.name}\);");
            Match match = regex.Match(generatedCode, classStart);

            if (match.Success)
            {
                string cloneHelperId = match.Groups[1].Value;
                string listCreation = GetListCreation(schema, field, DeserializationOptions.GreedyMutable);
                generatedCode = regex.Replace(generatedCode, $@"
                    IList<{listElement}>? _{field.name} = FlatSharp.Compiler.Generated.CloneHelpers_{cloneHelperId}.Clone(source.{field.name} as IList<{listElement}>);
                    this.{field.name} = _{field.name} != null ? {listCreation}(_{field.name}) : null;
                ", 1, match.Index);
            }

            return generatedCode;
        }

        private static string GetListCreation(Schema schema, Field field, string deserializationOption)
        {
            string listType = schema.GetCSharpType(field.type).TrimEnd('?');

            if (deserializationOption == DeserializationOptions.Lazy
            || deserializationOption == DeserializationOptions.Progressive
            || deserializationOption == DeserializationOptions.Greedy)
            {
                return $"{listType}.AsImmutable";
            }
            else if (schema.TypeIsReferenceTypeList(field.type) || schema.TypeIsUnionList(field.type))
            {
                return $"{listType}.ShallowCopy";
            }
            else
            {
                return $"new {listType}";
            }
        }

        private static int GetIndexValueOfField(string generatedCode, Schema schema, Field field, int tableReaderStart, int tableReaderEnd, string customPropertyRegex = null)
        {
            int propertyIndex = GetPropertyIndexOfField(generatedCode, schema, field, tableReaderStart, tableReaderEnd, customPropertyRegex);
            Regex regex = new Regex(@"(index|Index)(\d+)Value");
            Match match = regex.Match(generatedCode, propertyIndex);
            return match.Success ? int.Parse(match.Groups[2].Value) : -1;
        }

        private static int GetPropertyIndexOfField(string generatedCode, Schema schema, Field field, int tableReaderStart, int tableReaderEnd, string customPropertyRegex = null)
        {
            string listType = schema.GetCSharpType(field.type).TrimEnd('?');
            Regex regex = new Regex(customPropertyRegex ?? $@"public override {listType}\?* \b{field.name}\b");
            Match match = regex.Match(generatedCode, tableReaderStart, tableReaderEnd - tableReaderStart + 1);
            return match.Success ? match.Index : -1;
        }

        private static string ReplaceAllInRange(string source, string oldValue, string newValue, int startIndex, int endIndex)
        {
            Regex regex = new Regex(oldValue);
            Match match = regex.Match(source, startIndex, endIndex - startIndex + 1);

            while (match.Success && startIndex + match.Value.Length - 1 <= endIndex)
            {
                source = regex.Replace(source, newValue, 1, startIndex);
                match = regex.Match(source, match.Index + newValue.Length, endIndex - (match.Index + newValue.Length) + 1);
            }

            return source;
        }

        private static void ForEachMatch(ref string source, string pattern, Action<int> onMatch)
        {
            Regex regex = new Regex(pattern);
            Match match = regex.Match(source);

            while (match.Success)
            {
                onMatch(match.Index);
                match = regex.Match(source, match.Index + 1);
            }
        }

        private static string FirstOf(string source, IList<string> keywords, int startIndex, int endIndex)
        {
            string first = null;
            int firstIndex = source.Length;

            foreach (string keyword in keywords)
            {
                int index = source.IndexOf(keyword, startIndex, endIndex - startIndex + 1);

                if (index >= startIndex && index < firstIndex)
                {
                    first = keyword;
                    firstIndex = index;
                }
            }

            return first;
        }

        private static (int, int) GetIndexesOfNextBrackets(string source, int startIndex = 0)
        {
            int level = 1;
            int startBracketIndex = source.IndexOf('{', startIndex);
            int endBracketIndex = -1;
            int currentIndex = startBracketIndex;

            if (startBracketIndex == -1)
            {
                return (-1, -1);
            }

            while (level > 0)
            {
                int currentStartBracketIndex = source.IndexOf('{', currentIndex + 1);
                int currentEndBracketIndex = source.IndexOf('}', currentIndex + 1);

                if (currentEndBracketIndex == -1)
                {
                    endBracketIndex = -1;
                    break;
                }
                else if (currentStartBracketIndex < currentEndBracketIndex)
                {
                    level++;
                    currentIndex = currentStartBracketIndex;
                }
                else
                {
                    level--;
                    currentIndex = currentEndBracketIndex;
                    endBracketIndex = currentEndBracketIndex;
                }
            }

            return (startBracketIndex, endBracketIndex);
        }

        private static string GetTableReaderRegex(reflection.Object obj) => $@"class tableReader_.+_({String.Join('|', DeserializationOptions.AllWithoutEnumName)})<TInputBuffer>\s*:\s*global::{obj.name}\s*,";

        private static string GetClassDefinitionRegex(reflection.Object obj) => $@"namespace {obj.GetNamespace()}\s*{{(\s*\[.*\]\s*)*\s*.*class {obj.GetNameWithoutNamespace()}\s*:\s*object";

        private static bool TableReaderIsLazy(string source, int tableReaderStart, int tableReaderEnd) =>
            FirstOf(source, DeserializationOptions.All, tableReaderStart, tableReaderEnd) == DeserializationOptions.Lazy;

        private static bool TableReaderIsProgressive(string source, int tableReaderStart, int tableReaderEnd) =>
            FirstOf(source, DeserializationOptions.All, tableReaderStart, tableReaderEnd) == DeserializationOptions.Progressive;

        private static bool TableReaderIsGreedy(string source, int tableReaderStart, int tableReaderEnd) =>
            FirstOf(source, DeserializationOptions.All, tableReaderStart, tableReaderEnd) == DeserializationOptions.Greedy;

        private static bool TableReaderIsGreedyMutable(string source, int tableReaderStart, int tableReaderEnd) =>
            FirstOf(source, DeserializationOptions.All, tableReaderStart, tableReaderEnd) == DeserializationOptions.GreedyMutable;
    }
}