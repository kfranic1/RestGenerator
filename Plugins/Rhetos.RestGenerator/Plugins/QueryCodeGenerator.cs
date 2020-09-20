﻿/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System.ComponentModel.Composition;

namespace Rhetos.RestGenerator.Plugins
{
    [Export(typeof(IRestGeneratorPlugin))]
    [ExportMetadata(MefProvider.Implements, typeof(QueryInfo))]
    public class QueryCodeGenerator : IRestGeneratorPlugin
    {
        private readonly FilterSnippets _filterSnippets;

        public QueryCodeGenerator(FilterSnippets filterSnippets)
        {
            _filterSnippets = filterSnippets;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (QueryInfo)conceptInfo;

            if (DataStructureCodeGenerator.IsTypeSupported(info.DataStructure))
                codeBuilder.InsertCode(
                    _filterSnippets.ExpectedFilterTypesSnippet(info.DataStructure, info.Parameter),
                    DataStructureCodeGenerator.FilterTypesTag, info.DataStructure);
        }
    }
}
