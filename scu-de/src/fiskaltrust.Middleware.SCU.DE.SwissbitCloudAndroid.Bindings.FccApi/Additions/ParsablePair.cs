using Android.Runtime;
using Java.Interop;
using System;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid.Bindings.Additions
{
    public class ParsablePair : Java.Lang.Object, Java.IO.ISerializable, IJavaObject, IDisposable, IJavaPeerable
    {
        //[Register(".ctor", "(Ljava/lang/Object;Ljava/lang/Object;)V", "")]
        public ParsablePair(Java.Lang.Object first, Java.Lang.Object second)
        {
            First = first;
            Second = second;
        }

        public ParsablePair() { }

        public Java.Lang.Object First { get; }
        public Java.Lang.Object Second { get; }

        //[Register("component1", "()Ljava/lang/Object;", "")]
        public Java.Lang.Object Component1() => First;

        //[Register("component2", "()Ljava/lang/Object;", "")]
        public Java.Lang.Object Component2() => Second;
    }
}
