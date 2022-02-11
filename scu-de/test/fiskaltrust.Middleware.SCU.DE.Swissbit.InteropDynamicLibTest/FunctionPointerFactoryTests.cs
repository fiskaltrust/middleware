using System;
using System.Runtime.InteropServices;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.InteropDynamicLibTest
{
    public class FunctionPointerFactoryTests
    {
        [Fact]
        public void Instantiate()
        {
            var sut = new Interop.DynamicLib.FunctionPointerFactory();
            sut.Should().NotBeNull();
        }

        [Fact]
        public void LoadLibrary()
        {
            var sut = new Interop.DynamicLib.FunctionPointerFactory();
            var functionPointers = sut.LoadLibrary();
            functionPointers.Should().NotBeNull();
        }

        [Fact]
        public void GetVersion()
        {
            var sut = new Interop.DynamicLib.FunctionPointerFactory();
            var functionPointers = sut.LoadLibrary();
            functionPointers.Should().NotBeNull();

            var version = Marshal.PtrToStringAnsi(functionPointers.func_worm_getVersion());
            version.Should().NotBeNull();
        }
    }
}
