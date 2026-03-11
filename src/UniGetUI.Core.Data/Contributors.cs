namespace UniGetUI.Core.Data
{
    public static class ContributorsData
    {
        public static string[] Contributors = File.ReadAllLines(
            Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Data", "Contributors.list")
        ).Where(x => x != "").ToArray();
    }
}
