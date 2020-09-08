namespace SimpleApi
{
    public class Item
    {
        public string Type { get; set; }
        public string ProductName { get; set; }
        public int[] Sales { get; set; }
        public TargetGroup[] TargetGroup { get; set; }
    }

    public class TargetGroup
    {
        public string Buyer { get; set; }
        public string SellingPitch { get; set; }
    }
}