using System;
using System.Collections.Generic;
using System.Text;

namespace gMKVToolnix
{
    public delegate void gMkvExtractMethod(Object parameterList);

    public class gMKVJob
    {
        private gMkvExtractMethod _ExtractMethod;
        public gMkvExtractMethod ExtractMethod { get { return _ExtractMethod; } }

        private List<Object> _ParametersList;
        public List<Object> ParametersList { get { return _ParametersList; } }

        public gMKVJob(gMkvExtractMethod argExtractMethod, List<Object> argParameters)
        {
            _ExtractMethod = argExtractMethod;
            _ParametersList = argParameters;
        }
    }
}
