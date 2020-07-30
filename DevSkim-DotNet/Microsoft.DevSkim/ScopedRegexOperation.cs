﻿using Microsoft.CST.OAT;
using Microsoft.CST.OAT.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim
{
    public class ScopedRegexOperation : OatOperation
    {
        private RegexOperation regexEngine;
        public ScopedRegexOperation(Analyzer analyzer) : base(Operation.Custom,analyzer)
        {
            CustomOperation = "ScopedRegex";
            OperationDelegate = ScopedRegexOperationDelegate;
            regexEngine = new RegexOperation(analyzer);
        }

        public OperationResult ScopedRegexOperationDelegate(Clause c, object? state1, object? state2, IEnumerable<ClauseCapture>? captures)
        {
            if (state1 is TextContainer tc && c is ScopedRegexClause src)
            {
                var matchList = new List<Boundary>();
                if (Analyzer != null)
                {
                    var res = regexEngine.OperationDelegate.Invoke(src, tc.Line, null, null);
                    if (res.Result && res.Capture is TypedClauseCapture<List<Match>> mc)
                    {
                        List<Boundary> boundaries = new List<Boundary>();
                        foreach (var match in mc.Result)
                        {
                            if (match is Match m)
                            {
                                Boundary translatedBoundary = new Boundary()
                                {
                                    Length = m.Length,
                                    Index = m.Index + tc.GetLineBoundary(tc.LineNumber).Index
                                };
                                // Should return only scoped matches
                                if (tc.ScopeMatch(src.Scopes, translatedBoundary))
                                {
                                    boundaries.Add(translatedBoundary);
                                }
                            }
                        }
                        var result = c.Invert ? boundaries.Count == 0 : boundaries.Count > 0;
                        return new OperationResult(result, result && c.Capture ? new TypedClauseCapture<List<Boundary>>(c, boundaries, state1) : null);
                    }
                }
            }
            return new OperationResult(false, null);
        }
    }
}
