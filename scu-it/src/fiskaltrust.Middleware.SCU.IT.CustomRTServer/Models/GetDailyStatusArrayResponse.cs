using System.Collections.Generic;

public class GetDailyStatusArrayResponse : CustomRTDetailedResponse
{
    public List<GetDailyStatusResponseContent> ArrayResponse { get; set; } = new List<GetDailyStatusResponseContent>();

}
