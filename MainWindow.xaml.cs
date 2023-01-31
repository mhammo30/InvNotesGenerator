using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.IO.Pipes;

namespace InvNotesGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private static readonly char[] nameSeparators = { ' ', '\n' };

        private static readonly string[] itemHeaders = { "Count", "Item", "Holder" };
        private static readonly string[] moneyHeaders = { "Player", "Currency", "Equivalent" };

        // Plat, Gold, Electrum, Silver, Copper
        private static readonly int[] coinValues = { 1000, 100, 50, 10, 1 };

        [GeneratedRegex("([0-9]{1,})g")]
        private static partial Regex GoldPieceRegex();

        private readonly Regex GoldPieces = GoldPieceRegex();

        [GeneratedRegex("([0-9]{1,})p")]
        private static partial Regex PlatPieceRegex();

        private readonly Regex PlatPieces = PlatPieceRegex();

        
        [GeneratedRegex("([0-9]{1,})e")]
        private static partial Regex ElecPieceRegex();

        private readonly Regex ElecPieces = ElecPieceRegex();

        
        [GeneratedRegex("([0-9]{1,})s")]
        private static partial Regex SilverPieceRegex();

        private readonly Regex SilverPieces = SilverPieceRegex();

        
        [GeneratedRegex("([0-9]{1,})c")]
        private static partial Regex CopperPieceRegex();

        private readonly Regex CopperPieces = CopperPieceRegex();

        [GeneratedRegex("^([0-9]+) ([^@]+) @([a-zA-Z0-9/(/)]+)$")]
        private static partial Regex ItemRegex();

        private readonly Regex ItemsRegex = ItemRegex();

        [GeneratedRegex("^# ?(.*)$")]
        private static partial Regex HeaderLineRegex();

        private readonly Regex HeaderRegex = HeaderLineRegex();

        internal string[]? players;

        internal int playerRounding = 0;

        public string output = "";


        public MainWindow()
        {

            InitializeComponent();
        }

        internal string SplitCurrency(int plat, int gold, int elec, int silver, int copper)
        {
            // cant split if we do have have any players
            if (players is null || players.Length == 0)
            {
                return "";
            }

            
            int totalValue = plat * 1000 + gold * 100 + elec * 50 + silver * 10 + copper;

            // must have a total value > 0 or nothing to split
            if (totalValue == 0)
            {
                return "";
            }

            // chuck in array for easier iteration
            int[] coinsAvailable = {plat, gold, elec, silver, copper };

            // x axis players, y axis currency, last y index is players total currency value
            int[,] splits = new int[players.Length,coinValues.Length + 1];

            int coinIndex = 0;
            int playerIndex = 0;

            while (totalValue > 0)
            {
                playerIndex = 0;

                // find the player with the least currency
                for (int i = 0; i < players.Length; i++) 
                { 
                    if (splits[i, coinValues.Length] < splits[playerIndex, coinValues.Length])
                    {
                        playerIndex = i;
                    }
                }

                // start with plat and use all the available coins before moving on to the next currency
                if (coinsAvailable[coinIndex] > 0)
                {

                    coinsAvailable[coinIndex]--;
                    splits[playerIndex % players.Length, coinIndex]++;
                    splits[playerIndex % players.Length, coinValues.Length] += coinValues[coinIndex];
                    totalValue -= coinValues[coinIndex];

                } else
                {
                    coinIndex++;
                }
            }

            // done sorting currency, find the player with the least currency so we can flag the output
            for (int i = 0; i < players.Length; i++)
            {
                if (splits[i, coinValues.Length] < splits[playerIndex, coinValues.Length])
                {
                    playerIndex = i;
                }
            }

            // calculate the longest player name to adjust padding
            int longestName = moneyHeaders[0].Length;
            foreach (string player in players)
            {
                if (player.Length > longestName)
                {
                    longestName = player.Length;
                }
            }

            // calculate the padding for all the currencies
            int[] moneyPadding = new int[coinValues.Length];
            for (int i = 0; i < coinValues.Length; i++)
            {
                for (int j = 0; j < players.Length; j++)
                {
                    int digits = splits[j, i] == 0 ? 1 : (int)Math.Log10(splits[j, i]) + 1;
                    moneyPadding[i] = digits > moneyPadding[i] ? digits : moneyPadding[i];
                }
            }
            // get the total padding of all the currencies so we can padd the headers
            int totalMoneyPadding = moneyPadding.Sum(x => x);

            int equivPadding = 0;
            // get the paddig for the equivilent currency coins
            for (int i = 0; i < players.Length; i++)
            {
                int digits = splits[i, coinValues.Length] == 0 ? 1 : (int)Math.Log10(splits[i, coinValues.Length] / 100);
                equivPadding = digits > equivPadding ? digits : equivPadding;
            }
            // calculate the header padding
            int equivHeaderPadding = moneyHeaders[2].Length > equivPadding + 8 ? moneyHeaders[2].Length : equivPadding + 8;

            // build the table headers
            string outstr = $"| {moneyHeaders[0].PadRight(longestName)} | " +
                $"{moneyHeaders[1].PadRight(totalMoneyPadding + 9)} | " +
                $"{moneyHeaders[2].PadRight(equivHeaderPadding)} |\n" +
                $"| {"".PadRight(longestName,'-')} | " +
                $"{"".PadRight(totalMoneyPadding + 9,'-')} | " +
                $"{"".PadRight(equivHeaderPadding,'-')} |\n";


            // build each row of the table
            for (int i = 0; i < players.Length; i++)
            {
                // playerRounding flags which player got shorted the last loot distribution so this time we start with them
                int p = (players.Length - playerRounding + i) % players.Length;
                outstr += $"| {players[i].PadRight(longestName)} | " +
                    $"{splits[p, 0].ToString().PadLeft(moneyPadding[0])}p " + // pad each currency column
                    $"{splits[p, 1].ToString().PadLeft(moneyPadding[1])}g " +
                    $"{splits[p, 2].ToString().PadLeft(moneyPadding[2])}e " +
                    $"{splits[p, 3].ToString().PadLeft(moneyPadding[3])}s " +
                    $"{splits[p, 4].ToString().PadLeft(moneyPadding[4])}c | " +
                    $"{(splits[p, coinValues.Length] / coinValues[1]).ToString().PadLeft(equivPadding)}g " +
                    $"{(splits[p, coinValues.Length] % coinValues[1]) / coinValues[3]}s " +
                    $"{splits[p, coinValues.Length] % coinValues[3]}c{"".PadRight(equivHeaderPadding - equivPadding - 7)}|"; // pad the final table border
                //outstr += $"[{splits[i, coinValues.Length]}]";
                outstr += p == playerIndex ? " (Rounding)\n" : "\n"; // flag the first player that is going to be shorted this time around
            }

            return outstr;
        }

        internal void ParsePlayersList(string text)
        {
            playerRounding = 0;
            players = text.Split(nameSeparators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < players.Length; ++i)
            {
                // the last player with an * is flagged by the user to say this player should be the start of the currency distribution
                if (players[i].StartsWith('*') || players[i].EndsWith('*'))
                {
                    players[i] = players[i].Trim('*');
                    playerRounding = i; 
                }
            }
        }

        internal void ParseItemsList(string text)
        {
            string[] lines = text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            string header = "";

            List<Tuple<int, string, string>> items = new();

            int CurrencyPlatinum = 0;

            int CurrencyGold = 0;

            int CurrencyElectrum = 0;

            int CurrencySilver = 0;

            int CurrencyCopper = 0;

            foreach (var line in lines)
            {
                // find all the currencies for the current line, could be multiple entires on a line
                if (PlatPieces.IsMatch(line))
                {
                    foreach (Match coins in PlatPieces.Matches(line))
                    {
                        if (int.TryParse(coins.Groups[1].Value, out int coinCount))
                        {
                            CurrencyPlatinum += coinCount;
                        }
                    }
                }
                if (GoldPieces.IsMatch(line))
                {
                    foreach (Match coins in GoldPieces.Matches(line))
                    {
                        if (int.TryParse(coins.Groups[1].Value, out int coinCount))
                        {
                            CurrencyGold += coinCount;
                        }
                    }
                }
                if (ElecPieces.IsMatch(line))
                {
                    foreach (Match coins in ElecPieces.Matches(line))
                    {
                        if (int.TryParse(coins.Groups[1].Value, out int coinCount))
                        {
                            CurrencyElectrum += coinCount;
                        }
                    }
                }
                if (SilverPieces.IsMatch(line))
                {
                    foreach (Match coins in SilverPieces.Matches(line))
                    {
                        if (int.TryParse(coins.Groups[1].Value, out int coinCount))
                        {
                            CurrencySilver += coinCount;
                        }
                    }
                }
                if (CopperPieces.IsMatch(line))
                {
                    foreach (Match coins in CopperPieces.Matches(line))
                    {
                        if (int.TryParse(coins.Groups[1].Value, out int coinCount))
                        {
                            CurrencyCopper += coinCount;
                        }
                    }
                }
                // items must follow a <# of items> <item name> @<holder> pattern
                // only 1 item per line
                if (ItemsRegex.IsMatch(line))
                {
                    MatchCollection parts = ItemsRegex.Matches(line);
                    items.Add(new(int.Parse(parts[0].Groups[1].Value), parts[0].Groups[2].Value, parts[0].Groups[3].Value));
                    continue;
                }
                // the header will be the last line startig with a #
                if (HeaderRegex.IsMatch(line))
                {
                    header = HeaderRegex.Match(line).Groups[1].Value;
                    continue;
                }
            }

            // calculate all the paddings for the columns
            string[] itemsLines = new string[items.Count];
            int longestCount = itemHeaders[0].Length;
            int longestItem = itemHeaders[1].Length;
            int longestName = itemHeaders[2].Length;
            for (int i = 0; i < items.Count; i++)
            {
                int countLen = items[i].Item1.ToString().Length;
                longestCount = longestCount >  countLen ? longestCount : countLen;
                longestItem = longestItem > items[i].Item2.Length ? longestItem : items[i].Item2.Length;
                longestName = longestName > items[i].Item3.Length ? longestName : items[i].Item3.Length;
            }
            // build the items table entries
            for(int i = 0; i < items.Count; i++)
            {
                itemsLines[i] = $"| {items[i].Item1.ToString().PadRight(longestCount)} | {items[i].Item2.PadRight(longestItem)} | {items[i].Item3.PadRight(longestName)} |\n";
            }
            // write the header and table headers
            output = $"```# {header}\n| " +
                $"{itemHeaders[0].PadRight(longestCount)} | " +
                $"{itemHeaders[1].PadRight(longestItem)} | " +
                $"{itemHeaders[2].PadRight(longestName)} |\n" +
                $"| {"".PadRight(longestCount,'-')} | " +
                $"{"".PadRight(longestItem,'-')} | " +
                $"{"".PadRight(longestName,'-')} |\n";

            // add the item lines to the output
            foreach (string line in itemsLines) { output += line ; }

            // setup the currency group header
            output += $"\n# Money ({CurrencyPlatinum}p {CurrencyGold}g {CurrencyElectrum}e {CurrencySilver}s {CurrencyCopper}c)\n";

            output += SplitCurrency(CurrencyPlatinum, CurrencyGold, CurrencyElectrum, CurrencySilver, CurrencyCopper);

            output += "```";

            OutputTextBox.Text = output;
        }

        private void Players_TextChanged(object sender, TextChangedEventArgs e)
        {
            ParsePlayersList(PlayersTextBox.Text);
            ParseItemsList(ItemsTextBox.Text);
        }

        private void Items_TextChanged(object sender, TextChangedEventArgs e)
        {
            ParsePlayersList(PlayersTextBox.Text);
            ParseItemsList(ItemsTextBox.Text);
        }

        private void Players_LostFocus(object sender, RoutedEventArgs e)
        {
            ParsePlayersList(PlayersTextBox.Text);
            ParseItemsList(ItemsTextBox.Text);
        }

        private void Items_LostFocus(object sender, RoutedEventArgs e)
        {
            ParsePlayersList(PlayersTextBox.Text);
            ParseItemsList(ItemsTextBox.Text);
        }
    }
}
