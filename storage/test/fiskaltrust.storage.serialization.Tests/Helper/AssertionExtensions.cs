using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace fiskaltrust.storage.serialization.UnitTest.Helper
{
    public static class AssertionExtensions
    {
        public static void BeUnorderedEqualTo<TExpectation>(this ObjectAssertions obj, TExpectation expectation)
        {
#if NET40
            obj.Subject.ShouldBeEquivalentTo(expectation);
#else
            obj.BeEquivalentTo(expectation);
#endif
        }


    }
}
