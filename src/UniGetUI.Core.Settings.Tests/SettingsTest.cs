using System.Text.Json;
using UniGetUI.Core.Data;

namespace UniGetUI.Core.SettingsEngine.Tests
{

    public sealed class SerializableTestSub
    {
        public SerializableTestSub(string s, int c) { sub = s; count = c; }
        public string sub { get; set; }
        public int count { get; set; }
    }
    public sealed class SerializableTest
    {
        public SerializableTest(string t, int c, SerializableTestSub s) { title = t; count = c; sub = s; }
        public string title { get; set; }
        public int count { get; set; }
        public SerializableTestSub sub { get; set; }
    }

    public class SettingsTest
    {
        [Theory]
        [InlineData("TestSetting1", true, false, false, true)]
        [InlineData("TestSetting2", true, false, false, false)]
        [InlineData("Test.Settings_with", true, false, true, true)]
        [InlineData("TestSettingEntry With A  Space", false, true, false, false)]
        [InlineData("ª", false, false, false, false)]
        [InlineData("VeryVeryLongTestSettingEntrySoTheClassCanReallyBeStressedOut", true, false, true, true)]
        public void TestBooleanSettings(string SettingName, bool st1, bool st2, bool st3, bool st4)
        {
            Settings.Set(SettingName, st1);
            Assert.Equal(st1, Settings.Get(SettingName));
            Assert.Equal(st1, File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, SettingName)));

            Settings.Set(SettingName, st2);
            Assert.Equal(st2, Settings.Get(SettingName));
            Assert.Equal(st2, File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, SettingName)));

            Settings.Set(SettingName, st3);
            Assert.Equal(st3, Settings.Get(SettingName));
            Assert.Equal(st3, File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, SettingName)));

            Settings.Set(SettingName, st4);
            Assert.Equal(st4, Settings.Get(SettingName));
            Assert.Equal(st4, File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, SettingName)));

            Settings.Set(SettingName, false); // Cleanup
            Assert.False(Settings.Get(SettingName));
            Assert.False(File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, SettingName)));
        }

        [Theory]
        [InlineData("TestSetting1", "RandomFirstValue", "RandomSecondValue", "", "RandomThirdValue")]
        [InlineData("ktjgshdfsd", "", "", "", "RandomThirdValue")]
        [InlineData("ª", "RandomFirstValue", "    ", "\t", "RandomThirdValue")]
        [InlineData("TestSettingEntry With A  Space", "RandomFirstValue", "", "", "")]
        [InlineData("VeryVeryLongTestSettingEntrySoTheClassCanReallyBeStressedOut", "\x00\x01\x02\u0003", "", "", "RandomThirdValue")]
        public void TestValueSettings(string SettingName, string st1, string st2, string st3, string st4)
        {
            Settings.SetValue(SettingName, st1);
            Assert.Equal(st1, Settings.GetValue(SettingName));
            Assert.Equal(st1 != "", File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, SettingName)));

            Settings.SetValue(SettingName, st2);
            Assert.Equal(st2, Settings.GetValue(SettingName));
            Assert.Equal(st2 != "", File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, SettingName)));

            Settings.SetValue(SettingName, st3);
            Assert.Equal(st3, Settings.GetValue(SettingName));
            Assert.Equal(st3 != "", File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, SettingName)));

            Settings.SetValue(SettingName, st4);
            Assert.Equal(st4, Settings.GetValue(SettingName));
            Assert.Equal(st4 != "", File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, SettingName)));

            Settings.Set(SettingName, false); // Cleanup
            Assert.False(Settings.Get(SettingName));
            Assert.Equal("", Settings.GetValue(SettingName));
            Assert.False(File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, SettingName)));
        }

        [Theory]
        [InlineData("lsTestSetting1", new string[] { "UpdatedFirstValue", "RandomString1", "RandomTestValue", "AnotherRandomValue" }, new int[] { 9, 15, 21, 1001, 4567 }, new string[] { "itemA", "itemB", "itemC" })]
        [InlineData("lsktjgshdfsd", new string[] { "newValue1", "updatedString", "emptyString", "randomSymbols@123" }, new int[] { 42, 23, 17, 98765, 3482 }, new string[] { "itemX", "itemY", "itemZ" })]
        [InlineData("lsª", new string[] { "UniqueVal1", "NewVal2", "AnotherVal3", "TestVal4" }, new int[] { 123, 456, 789, 321, 654 }, new string[] { "item1", "item2", "item3" })]
        [InlineData("lsTestSettingEntry With A  Space", new string[] { "ChangedFirstValue", "AlteredSecondVal", "TestedValue", "FinalVal" }, new int[] { 23, 98, 456, 753, 951 }, new string[] { "testA", "testB", "testC" })]
        [InlineData("lsVeryVeryLongTestSettingEntrySoTheClassCanReallyBeStressedOut", new string[] { "newCharacterSet\x99\x01\x02", "UpdatedRandomValue", "TestEmptyString", "FinalTestValue" }, new int[] { 0b11001100, 1234, 5678, 1000000 }, new string[] { "finalTest1", "finalTest2", "finalTest3" })]
        public void TestListSettings(string SettingName, string[] ls1Array, int[] ls2Array, string[] ls3Array)
        {
            // Convert arrays to Lists manually
            List<string> ls1 = ls1Array.ToList();
            List<int> ls2 = ls2Array.ToList();
            List<SerializableTest> ls3 = [];
            foreach (var item in ls3Array.ToList())
            {
                ls3.Add(new SerializableTest(item, new Random().Next(), new SerializableTestSub(item + new Random().Next(), new Random().Next())));
            }

            Settings.ClearList(SettingName);
            Assert.Empty(Settings.GetList<object>(SettingName) ?? ["this shouldn't be null; something's wrong"]);
            Settings.SetList(SettingName, ls1);
            Assert.NotEmpty(Settings.GetList<string>(SettingName) ?? []);
            Assert.Equal(ls1[0], Settings.GetListItem<string>(SettingName, 0));
            Assert.Equal(ls1[2], Settings.GetListItem<string>(SettingName, 2));
            Assert.True(Settings.ListContains(SettingName, ls1[0]));
            Assert.False(Settings.ListContains(SettingName, "this is not a test case"));
            Assert.True(Settings.RemoveFromList(SettingName, ls1[0]));
            Assert.False(Settings.ListContains(SettingName, ls1[0]));
            Assert.False(Settings.RemoveFromList(SettingName, ls1[0]));
            Assert.False(Settings.ListContains(SettingName, ls1[0]));
            Assert.Equal(ls1[2], Settings.GetListItem<string>(SettingName, 1));
            Settings.AddToList(SettingName, "this is now a test case");
            Assert.Equal("this is now a test case", Settings.GetListItem<string>(SettingName, 3));
            Assert.Null(Settings.GetListItem<string>(SettingName, 4));

            Assert.Equal(Settings.GetListItem<string>(SettingName, 0), JsonSerializer.Deserialize<List<string>>(File.ReadAllText(Path.Join(CoreData.UniGetUIDataDirectory, $"{SettingName}.json")))[0]);

            Settings.ClearList(SettingName);
            Assert.Empty(Settings.GetList<object>(SettingName) ?? ["this shouldn't be null; something's wrong"]);

            Settings.SetList(SettingName, ls2);
            Assert.NotEmpty(Settings.GetList<int>(SettingName) ?? []);
            Assert.Equal(ls2[0], Settings.GetListItem<int>(SettingName, 0));
            Assert.False(Settings.ListContains(SettingName, -12000));
            Assert.True(Settings.ListContains(SettingName, ls2[3]));
            Assert.True(Settings.RemoveFromList(SettingName, ls2[0]));
            Assert.False(Settings.ListContains(SettingName, ls2[0]));
            Assert.False(Settings.RemoveFromList(SettingName, ls2[0]));
            Assert.False(Settings.ListContains(SettingName, ls2[0]));

            Settings.SetList(SettingName, ls3);
            Assert.Equal(ls3.Count, Settings.GetList<SerializableTest>(SettingName)?.Count);
            Assert.Equal(ls3[1].sub.sub, Settings.GetListItem<SerializableTest>(SettingName, 1)?.sub.sub);
            Assert.True(Settings.RemoveFromList(SettingName, ls3[0]));
            Assert.False(Settings.RemoveFromList(SettingName, ls3[0]));
            Assert.Equal(ls3[1].sub.sub, Settings.GetListItem<SerializableTest>(SettingName, 0)?.sub.sub);
            Settings.ClearList(SettingName); // Cleanup
            Assert.Empty(Settings.GetList<object>(SettingName) ?? ["this shouldn't be null; something's wrong"]);
            Assert.False(File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, $"{SettingName}.json")));
        }

        [Theory]
        [InlineData("dTestSetting1", new string[] { "UpdatedFirstValue", "RandomString1", "RandomTestValue", "AnotherRandomValue" }, new int[] { 9, 15, 21, 1001, 4567 }, new string[] { "itemA", "itemB", "itemC" })]
        [InlineData("dktjgshdfsd", new string[] { "newValue1", "updatedString", "emptyString", "randomSymbols@123" }, new int[] { 42, 23, 17, 98765, 3482 }, new string[] { "itemX", "itemY", "itemZ" })]
        [InlineData("dª", new string[] { "UniqueVal1", "NewVal2", "AnotherVal3", "TestVal4" }, new int[] { 123, 456, 789, 321, 654 }, new string[] { "item1", "item2", "item3" })]
        [InlineData("dTestSettingEntry With A  Space", new string[] { "ChangedFirstValue", "AlteredSecondVal", "TestedValue", "FinalVal" }, new int[] { 23, 98, 456, 753, 951 }, new string[] { "testA", "testB", "testC" })]
        [InlineData("dVeryVeryLongTestSettingEntrySoTheClassCanReallyBeStressedOut", new string[] { "newCharacterSet\x99\x01\x02", "UpdatedRandomValue", "TestEmptyString", "FinalTestValue" }, new int[] { 0b11001100, 1234, 5678, 1000000 }, new string[] { "finalTest1", "finalTest2", "finalTest3" })]
        public void TestDictionarySettings(string SettingName, string[] keyArray, int[] intArray, string[] strArray)
        {
            Dictionary<string, SerializableTest?> test = [];
            Dictionary<string, string> emptyDictionary = [];
            Dictionary<string, SerializableTest?> nonEmptyDictionary = [];
            nonEmptyDictionary["this should not be null; something's wrong"] = null;

            for (int idx = 0; idx < keyArray.Length; idx++)
            {
                test[keyArray[idx]] = new SerializableTest(
                    strArray[idx % strArray.Length],
                    intArray[idx % intArray.Length],
                    new SerializableTestSub(
                        strArray[(idx + 1) % strArray.Length],
                        intArray[(idx + 1) % intArray.Length]
                    )
                );
            }

            string randStr = new Random().Next().ToString();
            Settings.SetDictionaryItem(randStr, "key", 12);
            Assert.Equal(12, Settings.GetDictionaryItem<string, int>(randStr, "key"));
            Settings.SetDictionary(SettingName, test);
            Assert.Equal(JsonSerializer.Serialize(test), File.ReadAllLines(Path.Join(CoreData.UniGetUIDataDirectory, $"{SettingName}.json"))[0]);
            Assert.Equal(test[keyArray[0]]?.sub.count, Settings.GetDictionary<string, SerializableTest?>(SettingName)?[keyArray[0]]?.sub.count);
            Assert.Equal(test[keyArray[1]]?.sub.count, Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, keyArray[1])?.sub.count);
            Settings.SetDictionaryItem(SettingName, keyArray[0], test[keyArray[1]]);
            Assert.Equal(test[keyArray[1]]?.sub.count, Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, keyArray[0])?.sub.count);
            Assert.NotEqual(test[keyArray[0]]?.sub.count, Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, keyArray[0])?.sub.count);
            Assert.Equal(test[keyArray[1]]?.sub.count, Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, keyArray[1])?.sub.count);
            Assert.Equal(test[keyArray[1]]?.count, Settings.SetDictionaryItem(
                SettingName,
                keyArray[0],
                new SerializableTest(
                    "this is not test data",
                    -12000,
                    new SerializableTestSub("this sub is not test data", -13000)
                )
            )?.count);
            Assert.Equal(-12000, Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, keyArray[0])?.count);
            Assert.Equal("this is not test data", Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, keyArray[0])?.title);
            Assert.Equal(-13000, Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, keyArray[0])?.sub.count);
            Settings.SetDictionaryItem(SettingName, "this is not a test data key", test[keyArray[0]]);
            Assert.Equal(test[keyArray[0]]?.title, Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, "this is not a test data key")?.title);
            Assert.Equal(test[keyArray[0]]?.sub.count, Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, "this is not a test data key")?.sub.count);
            Assert.True(Settings.DictionaryContainsKey<string, SerializableTest?>(SettingName, "this is not a test data key"));
            Assert.True(Settings.DictionaryContainsValue<string, SerializableTest?>(SettingName, test[keyArray[0]]));
            Assert.NotNull(Settings.RemoveDictionaryKey<string, SerializableTest?>(SettingName, "this is not a test data key"));
            Assert.Null(Settings.RemoveDictionaryKey<string, SerializableTest?>(SettingName, "this is not a test data key"));
            Assert.False(Settings.DictionaryContainsKey<string, SerializableTest?>(SettingName, "this is not a test data key"));
            Assert.False(Settings.DictionaryContainsValue<string, SerializableTest?>(SettingName, test[keyArray[0]]));
            Assert.True(Settings.DictionaryContainsValue<string, SerializableTest?>(SettingName, test[keyArray[2]]));

            Assert.Equal(
                JsonSerializer.Serialize(Settings.GetDictionary<string, SerializableTest>(SettingName)),
                File.ReadAllLines(Path.Join(CoreData.UniGetUIDataDirectory, $"{SettingName}.json"))[0]
            );

            Settings.ClearDictionary(SettingName); // Cleanup
            Assert.Empty(Settings.GetDictionary<string, SerializableTest?>(SettingName) ?? nonEmptyDictionary);
            Assert.False(File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, $"{SettingName}.json")));
        }
    }
}
