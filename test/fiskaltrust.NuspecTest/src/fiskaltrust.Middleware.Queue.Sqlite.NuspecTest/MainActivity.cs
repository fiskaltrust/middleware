using Android.App;
using Android.OS;
using Android.Support.V7.App;

namespace fiskaltrust.Middleware.Queue.Sqlite.NuspecTest
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
        }
    }
}
