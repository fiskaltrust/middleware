using Android.OS;

namespace DE.Fiskal.Connector.Android.Api.Model
{
    public sealed partial class InitData
    {
        public sealed partial class Creator
        {
            Java.Lang.Object IParcelableCreator.CreateFromParcel(Parcel source) => CreateFromParcel(source);

            Java.Lang.Object[] IParcelableCreator.NewArray(int size) => NewArray(size);
        }
    }
}
