using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;

namespace HypixelBazaar
{
	public partial class Form1 : Form
	{
		public static Dictionary<string, Item> itemDict;
		public static List<Item> fixedItems;
		public static List<string> dontRecurse = new List<string> {"REDSTONE", "IRON_INGOT", "GOLD_INGOT", "SLIME_BALL", "DIAMOND", "COAL", "WOOL", "INK_SACK-4", "WHEAT", "EMERALD"};
		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			itemDict = new Dictionary<string, Item>();
			GetRecipes();
		}

		private async void GetRecipes()
		{
			DirectoryInfo d = new DirectoryInfo(@"..\..\items");
			FileInfo[] Files = d.GetFiles("*.json");
			foreach (FileInfo file in Files)
			{
				using (StreamReader reader = new StreamReader(file.FullName))
				{
					JObject WholeAssJSON = JObject.Parse(await reader.ReadToEndAsync());
					JToken jTokenRecipe = WholeAssJSON["recipe"];
					string internalName = WholeAssJSON["internalname"].ToString();
					Item newItem = new Item
					{
						ProductID = internalName,
						SellPrice = float.MinValue,
						BuyPrice = float.MinValue,
						MadeFrom = new Dictionary<string, int>()
					};
					if (jTokenRecipe != null)
					{
						foreach (JToken recipeItemAndPosition in jTokenRecipe)
						{
							JToken jTokenRecipeItem = recipeItemAndPosition.Children().ElementAt(0);
							string recipeItem = jTokenRecipeItem.ToString();
							int lastIndexOfColon = recipeItem.LastIndexOf(":");
							if (lastIndexOfColon != -1)
							{
								string recipeItemName = recipeItem.Substring(0, lastIndexOfColon);
								string recipeItemAmount = recipeItem.Substring(lastIndexOfColon + 1, recipeItem.Length - lastIndexOfColon - 1);
								if (!newItem.MadeFrom.ContainsKey(recipeItemName))
								{
									newItem.MadeFrom[recipeItemName] = 0;
								}
								newItem.MadeFrom[recipeItemName] += int.Parse(recipeItemAmount);
							}
						}
					}
					itemDict[newItem.ProductID] = newItem;
				}
			}
			Console.WriteLine("Finished Adding Blank Recipes");
			GetPrices();
		}

		private async void GetPrices()
		{
			HttpClient httpClient = new HttpClient();
			HttpContent content = (await httpClient.GetAsync("https://api.hypixel.net/skyblock/bazaar?key=1d9ee49d-8fba-4bc1-9c3e-4c1d23477df8")).Content;
			using (StreamReader reader = new StreamReader(await content.ReadAsStreamAsync()))
			{
				JObject WholeAssJSON = JObject.Parse(await reader.ReadToEndAsync());
				JToken products = WholeAssJSON["products"];
				foreach(KeyValuePair<string, Item> kvp in itemDict)
				{
					JToken token = products[kvp.Key.Replace("-",":")];
					if (token != null)
					{
						JToken quickStatus = token["quick_status"];
						if (quickStatus != null)
						{
							itemDict[kvp.Key].SellPrice = float.Parse(quickStatus["sellPrice"].ToString());
							itemDict[kvp.Key].BuyPrice = float.Parse(quickStatus["buyPrice"].ToString());
						}
					}
				}
			}
			Console.WriteLine("Finished Adding Prices to Recipes");
			FixPrices();
		}

		/// <summary>
		/// Fixes the recursive prices
		/// </summary>
		private void FixPrices()
		{
			//TODO: Verify that when removing recipes, the best recipe is deleted.
			//e.g, if buying a block of redstone is cheaper than buying 9 redstone,
			//keep the buy redstone block recipe
			foreach (string name in dontRecurse)
			{
				itemDict[name].MadeFrom = new Dictionary<string, int>();
			}
			fixedItems = itemDict.Values.ToList();
			Console.WriteLine("Finished Fixing Prices For Infinite Recursion");
			RemoveUnbuyables();
		}

		/// <summary>
		/// Removes items without a buying value
		/// </summary>
		private void RemoveUnbuyables()
		{

			fixedItems.RemoveAll(NoBuy);
			RemoveUnsellables();
		}

		/// <summary>
		/// Removes items without a selling value
		/// </summary>
		private void RemoveUnsellables()
		{
			fixedItems.RemoveAll(NoSell);
			TestPrices();
		}

		private void TestPrices()
		{
			List<Item> testingList = new List<Item>();
			foreach (Item item in fixedItems)
			{
				//Console.WriteLine("Processing Whole Item: " + kvp.Key);
				if(item.Profit > 0 && item.BuyAndManufacturePrice > 0)
				{
					testingList.Add(item);
				}
			}
			Item[] betterItems = testingList.ToArray();
			Array.Sort(betterItems, ProfitSort);
			Array.Sort(betterItems, ProfitSort);
			Array.Sort(betterItems, ProfitSort);
			foreach (Item item in betterItems)
			{
				Console.WriteLine(item.ToString());
			}
			WhateverElse();
		}

		private void WhateverElse()
		{
			Console.WriteLine("Finished Everything");
		}

		private int ProfitSort(Item c1, Item c2)
		{
			return c1.Profit.CompareTo(c2.Profit);
		}

		private static bool NoBuy(Item item)
		{
			return item.BuyAndManufacturePrice == float.MinValue;
		}

		private static bool NoSell(Item item)
		{
			return item.SellPrice == float.MinValue;
		}
	}
}
