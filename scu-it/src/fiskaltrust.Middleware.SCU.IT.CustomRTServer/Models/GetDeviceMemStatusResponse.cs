public class GetDeviceMemStatusResponse : CustomRTDetailedResponse
{
    public int ej_capacity { get; set; }
    public int ej_used { get; set; }
    public int ej_available { get; set; }
    public int average_erase_count { get; set; }
}
