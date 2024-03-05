using Android.OS;

namespace DE.Fiskal.Connector.Android.Api.Exception
{
    public sealed partial class TssTokenException
    {
        public sealed partial class Creator
        {
            Java.Lang.Object IParcelableCreator.CreateFromParcel(Parcel source) => source;

            Java.Lang.Object[] IParcelableCreator.NewArray(int size) => new Java.Lang.Object[size];
        }
    }
}
