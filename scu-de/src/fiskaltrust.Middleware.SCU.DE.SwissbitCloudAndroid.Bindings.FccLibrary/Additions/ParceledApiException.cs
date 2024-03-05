using Android.OS;

namespace DE.Fiskal.Connector.Android.Client.Library.Aidl
{
    public sealed partial class ParceledApiException
    {
        public sealed partial class Creator
        {
            Java.Lang.Object IParcelableCreator.CreateFromParcel(Parcel source) => CreateFromParcel(source);

            Java.Lang.Object[] IParcelableCreator.NewArray(int size) => NewArray(size);
        }
    }
}
