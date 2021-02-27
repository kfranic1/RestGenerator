﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rhetos.Host.AspNet.RestApi.Metadata
{
    public class DataStructureInfoMetadata : ConceptInfoRestMetadata
    {
        private readonly Tuple<string, Type>[] parameters;

        public DataStructureInfoMetadata(Tuple<string, Type>[] parameters)
        {
            this.parameters = parameters;
        }

        public Tuple<string, Type>[] GetParameters()
        {
            return parameters;
        }
    }
}
