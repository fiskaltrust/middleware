public class ResponseBodyErrory
{
    public int err_fm_present { get; set; }
    public int err_ej_present { get; set; }
    public int err_mkey_present { get; set; }
    public int err_mkey_valid { get; set; }
    public int err_ej_full { get; set; }
    public int err_fm_full { get; set; }
    public int err_hwinit_max { get; set; }
    public int err_cert_expired { get; set; }
    public int err_count { get; set; }

    public int warn_ej_full { get; set; }
    public int warn_fm_full { get; set; }
    public int warn_hwinit_max { get; set; }
    public int warn_cert_expired { get; set; }
    public int warn_count { get; set; }
    public int warn_hwinit_val { get; set; }
    public int warn_fm_full_val { get; set; }
    public int warn_ej_full_val { get; set; }

    public int err_fm_status { get; set; }
}
