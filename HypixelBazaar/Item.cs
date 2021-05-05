using System;
using System.Collections.Generic;

namespace HypixelBazaar
{
	public class Item
	{
		public string ProductID { get; set; }
		
		public float SellPrice { get; set; }

		public float BuyPrice { get; set; }

		private float buyAndManufacturePrice = float.MinValue;

		public float BuyAndManufacturePrice {
			get
			{
				if (buyAndManufacturePrice == float.MinValue)
				{
					if (MadeFrom.Count == 0)
					{
						buyAndManufacturePrice = BuyPrice;
					}
					else
					{
						float manufacturePrice = 0;
						foreach (KeyValuePair<string, int> kvp in MadeFrom)
						{
							manufacturePrice += Form1.itemDict[kvp.Key].BuyAndManufacturePrice * kvp.Value;
						}
						// Item that you can't buy
						if (BuyPrice == float.MinValue)
						{
							buyAndManufacturePrice = manufacturePrice;
						}
						else
						{
							buyAndManufacturePrice = Math.Min(BuyPrice, manufacturePrice);
						}
					}
				}
				return buyAndManufacturePrice;
			}
		}

		public float Profit => SellPrice - BuyAndManufacturePrice;

		public Dictionary<string, int> MadeFrom { get; set; }


		public override string ToString()
		{
			return ProductID + ", Buy Total:" + BuyAndManufacturePrice + ", Sell: " + SellPrice + ", Profit: " + Profit;
		}

	}
}
