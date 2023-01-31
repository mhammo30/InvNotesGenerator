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

        private static readonly string[] itemHeaders = { "Count", "Item", "Holder" };
        private static readonly string[] moneyHeaders = { "Player", "Currency", "Equivalent" };
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
            if (players is null || players.Length == 0)
            {
                return "";
            }
            int totalValue = plat * 1000 + gold * 100 + elec * 50 + silver * 10 + copper;

            if (totalValue == 0)
            {
                return "";
            }

            int[] coinsAvailable = {plat, gold, elec, silver, copper };

            int[,] splits = new int[players.Length,coinValues.Length + 1];

            int coinIndex = 0;
            int playerIndex = 0;
            while (totalValue > 0)
            {
                playerIndex = 0;

                for (int i = 0; i < players.Length; i++) 
                { 
                    if (splits[i, coinValues.Length] < splits[playerIndex, coinValues.Length])
                    {
                        playerIndex = i;
                    }
                }

                /*while (splits[playerIndex % players.Length, coinValues.Length] > splits[(playerIndex + 1) % players.Length, coinValues.Length])
                {
                    playerIndex++;
                }*/ 

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

            for (int i = 0; i < players.Length; i++)
            {
                if (splits[i, coinValues.Length] < splits[playerIndex, coinValues.Length])
                {
                    playerIndex = i;
                }
            }

            int longestName = moneyHeaders[0].Length;
            foreach (string player in players)
            {
                if (player.Length > longestName)
                {
                    longestName = player.Length;
                }
            }

            int[] moneyPadding = new int[coinValues.Length];
            for (int i = 0; i < coinValues.Length; i++)
            {
                for (int j = 0; j < players.Length; j++)
                {
                    int digits = splits[j, i] == 0 ? 1 : (int)Math.Log10(splits[j, i]) + 1;
                    moneyPadding[i] = digits > moneyPadding[i] ? digits : moneyPadding[i];
                }
            }
            int totalMoneyPadding = moneyPadding.Sum(x => x);
            int equivPadding = 0;

            for (int i = 0; i < players.Length; i++)
            {
                int digits = splits[i, coinValues.Length] == 0 ? 1 : (int)Math.Log10(splits[i, coinValues.Length] / 100);
                equivPadding = digits > equivPadding ? digits : equivPadding;
            }

            int equivHeaderPadding = moneyHeaders[2].Length > equivPadding + 8 ? moneyHeaders[2].Length : equivPadding + 8;

            string outstr = $"| {moneyHeaders[0].PadRight(longestName)} | " +
                $"{moneyHeaders[1].PadRight(totalMoneyPadding + 9)} | " +
                $"{moneyHeaders[2].PadRight(equivHeaderPadding)} |\n" +
                $"| {"".PadRight(longestName,'-')} | " +
                $"{"".PadRight(totalMoneyPadding + 9,'-')} | " +
                $"{"".PadRight(equivHeaderPadding,'-')} |\n";

            for (int i = 0; i < players.Length; i++)
            {
                int p = (players.Length - playerRounding + i) % players.Length;
                outstr += $"| {players[i].PadRight(longestName)} | " +
                    $"{splits[p, 0].ToString().PadLeft(moneyPadding[0])}p " +
                    $"{splits[p, 1].ToString().PadLeft(moneyPadding[1])}g " +
                    $"{splits[p, 2].ToString().PadLeft(moneyPadding[2])}e " +
                    $"{splits[p, 3].ToString().PadLeft(moneyPadding[3])}s " +
                    $"{splits[p, 4].ToString().PadLeft(moneyPadding[4])}c | " +
                    $"{(splits[p, coinValues.Length] / coinValues[1]).ToString().PadLeft(equivPadding)}g " +
                    $"{(splits[p, coinValues.Length] % coinValues[1]) / coinValues[3]}s " +
                    $"{splits[p, coinValues.Length] % coinValues[3]}c{"".PadRight(equivHeaderPadding - equivPadding - 7)}|";
                //outstr += $"[{splits[i, coinValues.Length]}]";
                outstr += p == playerIndex ? " (Rounding)\n" : "\n";
            }

            return outstr;
        }

        internal void ParsePlayersList(string text)
        {
            playerRounding = 0;
            players = text.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < players.Length; ++i)
            {
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
                if (ItemsRegex.IsMatch(line))
                {
                    MatchCollection parts = ItemsRegex.Matches(line);
                    items.Add(new(int.Parse(parts[0].Groups[1].Value), parts[0].Groups[2].Value, parts[0].Groups[3].Value));
                    continue;
                }
                if (HeaderRegex.IsMatch(line))
                {
                    header = HeaderRegex.Match(line).Groups[1].Value;
                    continue;
                }
            }

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
            for(int i = 0; i < items.Count; i++)
            {
                itemsLines[i] = $"| {items[i].Item1.ToString().PadRight(longestCount)} | {items[i].Item2.PadRight(longestItem)} | {items[i].Item3.PadRight(longestName)} |\n";
            }
            output = $"```# {header}\n| " +
                $"{itemHeaders[0].PadRight(longestCount)} | " +
                $"{itemHeaders[1].PadRight(longestItem)} | " +
                $"{itemHeaders[2].PadRight(longestName)} |\n" +
                $"| {"".PadRight(longestCount,'-')} | " +
                $"{"".PadRight(longestItem,'-')} | " +
                $"{"".PadRight(longestName,'-')} |\n";

            foreach (string line in itemsLines) { output += line ; }

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
