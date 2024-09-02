using Newtonsoft.Json;

public class Tim
{
    public string Team { get; set; }
    public string ISOCode { get; set; }
    public int FIBARanking { get; set; }
    public int Points { get; set; } = 0;
    public int Scored { get; set; } = 0;
    public int Conceded { get; set; } = 0;
    public int Wins { get; set; } = 0;
    public int Losses { get; set; } = 0;
    public int GoalDifference { get; set; } = 0;
    public string Group { get; set; } // Added to track group membership
}

public class Exhibition
{
    [JsonIgnore]
    public string Date { get; set; } // Ignoring the Date property

    public string Opponent { get; set; }
    public string Result { get; set; }
}

public class Group
{
    public string GroupName { get; set; }
    public List<Tim> Teams { get; set; }
}

public class Program
{
    public static List<Group> LoadGroups(string filePath)
    {
        var jsonData = File.ReadAllText(filePath);
        var groups = JsonConvert.DeserializeObject<Dictionary<string, List<Tim>>>(jsonData)
                        .Select(kvp => new Group { GroupName = kvp.Key, Teams = kvp.Value })
                        .ToList();
        foreach (var group in groups)
        {
            foreach (var team in group.Teams)
            {
                team.Group = group.GroupName; // Assign the group name to each team
            }
        }
        return groups;
    }

    public static Dictionary<string, List<Exhibition>> LoadExhibitions(string filePath)
    {
        var jsonData = File.ReadAllText(filePath);
        var exhibitions = JsonConvert.DeserializeObject<Dictionary<string, List<Exhibition>>>(jsonData);
        return exhibitions;
    }

    public static int CalculateWinProbability(Tim team1, Tim team2)
    {
        int rankingDifference = team2.FIBARanking - team1.FIBARanking;
        int baseProbability = 50;
        int winProbability = baseProbability + (rankingDifference * 2);

        return Math.Max(5, Math.Min(95, winProbability));
    }

    public static (Tim Winner, Tim Loser, int WinnerScore, int LoserScore) SimulateMatch(Tim team1, Tim team2)
    {
        int probability = CalculateWinProbability(team1, team2);
        Random random = new Random();
        int randomValue = random.Next(0, 100);

        Tim winner = randomValue < probability ? team1 : team2;
        Tim loser = winner == team1 ? team2 : team1;

        int winnerScore = random.Next(60, 100);
        int loserScore = winnerScore - random.Next(0, 15);

        winner.Points += 2;
        winner.Wins += 1;
        winner.Scored += winnerScore;
        winner.Conceded += loserScore;
        winner.GoalDifference += (winnerScore - loserScore);

        loser.Points += 1;
        loser.Losses += 1;
        loser.Scored += loserScore;
        loser.Conceded += winnerScore;
        loser.GoalDifference += (loserScore - winnerScore);

        return (winner, loser, winnerScore, loserScore);
    }

    public static Dictionary<string, Dictionary<string, List<(string Team1, string Team2, string Score)>>> SimulateGroupStage(List<Group> groups)
    {
        var results = new Dictionary<string, Dictionary<string, List<(string Team1, string Team2, string Score)>>> {
            { "I", new Dictionary<string, List<(string Team1, string Team2, string Score)>>() },
            { "II", new Dictionary<string, List<(string Team1, string Team2, string Score)>>() },
            { "III", new Dictionary<string, List<(string Team1, string Team2, string Score)>>() }
        };

        foreach (var group in groups)
        {
            var teams = group.Teams;
            var groupKey = group.GroupName;

            results["I"][groupKey] = new List<(string, string, string)>();
            results["II"][groupKey] = new List<(string, string, string)>();
            results["III"][groupKey] = new List<(string, string, string)>();

            var matches = new List<List<(Tim, Tim)>> {
                // I kolo
                new List<(Tim, Tim)> { (teams[0], teams[1]), (teams[2], teams[3]) },
                // II kolo
                new List<(Tim, Tim)> { (teams[0], teams[2]), (teams[1], teams[3]) },
                // III kolo
                new List<(Tim, Tim)> { (teams[0], teams[3]), (teams[1], teams[2]) }
            };

            for (int roundIndex = 0; roundIndex < matches.Count; roundIndex++)
            {
                string roundKey = roundIndex == 0 ? "I" : roundIndex == 1 ? "II" : "III";
                foreach (var match in matches[roundIndex])
                {
                    var matchResult = SimulateMatch(match.Item1, match.Item2);
                    results[roundKey][groupKey].Add((matchResult.Winner.Team, matchResult.Loser.Team, $"{matchResult.WinnerScore}:{matchResult.LoserScore}"));
                }
            }
        }

        return results;
    }

    public static Dictionary<string, List<Tim>> RankTeamsInGroups(List<Group> groups)
    {
        var finalRankings = new Dictionary<string, List<Tim>>();

        foreach (var group in groups)
        {
            group.Teams = group.Teams
                .OrderByDescending(t => t.Points)
                .ThenByDescending(t => t.GoalDifference)
                .ThenByDescending(t => t.Scored)
                .ToList();

            finalRankings[group.GroupName] = group.Teams;
        }

        return finalRankings;
    }

    public static void PrintGroupStageResults(Dictionary<string, Dictionary<string, List<(string Team1, string Team2, string Score)>>> results, Dictionary<string, List<Tim>> rankings)
    {
        Console.WriteLine("Group Stage Results:");

        foreach (var round in results.Keys)
        {
            Console.WriteLine($"\n{round} Round:");

            foreach (var group in results[round].Keys)
            {
                Console.WriteLine($"\n  Group {group}:");

                foreach (var match in results[round][group])
                {
                    Console.WriteLine($"    {match.Team1} - {match.Team2} ({match.Score})");
                }
            }
        }

        Console.WriteLine("\nFinal Group Standings:");
        foreach (var group in rankings.Keys)
        {
            Console.WriteLine($"\n  Group {group} (Team - Wins/Losses/Points/Scored/Conceded/Goal Difference):");

            for (int i = 0; i < rankings[group].Count; i++)
            {
                var team = rankings[group][i];
                Console.WriteLine($"    {i + 1}. {team.Team}  {team.Wins} / {team.Losses} / {team.Points} / {team.Scored} / {team.Conceded} / {(team.GoalDifference >= 0 ? "+" : "")}{team.GoalDifference}");
            }
        }
    }

    public static List<Tim> RankTeamsForDraw(Dictionary<string, List<Tim>> groupRankings)
    {
        var firstPlaceTeams = groupRankings.Values.Select(teams => teams[0]).ToList();
        var secondPlaceTeams = groupRankings.Values.Select(teams => teams[1]).ToList();
        var thirdPlaceTeams = groupRankings.Values.Select(teams => teams[2]).ToList();

        var rankedFirstPlaceTeams = firstPlaceTeams.OrderByDescending(t => t.Points).ThenByDescending(t => t.GoalDifference).ThenByDescending(t => t.Scored).ToList();
        var rankedSecondPlaceTeams = secondPlaceTeams.OrderByDescending(t => t.Points).ThenByDescending(t => t.GoalDifference).ThenByDescending(t => t.Scored).ToList();
        var rankedThirdPlaceTeams = thirdPlaceTeams.OrderByDescending(t => t.Points).ThenByDescending(t => t.GoalDifference).ThenByDescending(t => t.Scored).ToList();

        var finalRanking = new List<Tim>();
        finalRanking.AddRange(rankedFirstPlaceTeams);
        finalRanking.AddRange(rankedSecondPlaceTeams);
        finalRanking.AddRange(rankedThirdPlaceTeams);

        return finalRanking;
    }

    public static void PerformDraw(List<Tim> teamsForDraw)
    {
        var random = new Random();
        var hatD = teamsForDraw.Take(2).ToList();
        var hatE = teamsForDraw.Skip(2).Take(2).ToList();
        var hatF = teamsForDraw.Skip(4).Take(2).ToList();
        var hatG = teamsForDraw.Skip(6).Take(2).ToList();

        Console.WriteLine("\nŠešir D (najbolje dve prvoplasirane ekipe):");
        PrintTeams(hatD);

        Console.WriteLine("\nŠešir E (najbolje dve drugoplasirane ekipe):");
        PrintTeams(hatE);

        Console.WriteLine("\nŠešir F (najbolje dve trećeplasirane ekipe):");
        PrintTeams(hatF);

        Console.WriteLine("\nŠešir G (preostale ekipe):");
        PrintTeams(hatG);

    }

    public static void PerformEliminationPhase(List<Tim> teams)
    {
        var random = new Random();

        // Simulate Quarterfinals
        var quarterfinals = new List<(Tim, Tim)>();
        var remainingTeams = new List<Tim>(teams);

        Console.WriteLine("\nQuarterfinals Draw:");
        while (quarterfinals.Count < 4 && remainingTeams.Count >= 2)
        {
            Tim team1 = remainingTeams[random.Next(remainingTeams.Count)];
            remainingTeams.Remove(team1);

            Tim team2 = remainingTeams
                .Where(t => t.Group != team1.Group)
                .OrderBy(t => random.Next())
                .FirstOrDefault();

            if (team2 != null)
            {
                remainingTeams.Remove(team2);
                quarterfinals.Add((team1, team2));
                Console.WriteLine($"  {team1.Team} vs {team2.Team}");
            }
            else
            {
                remainingTeams.Add(team1);
            }
        }

        // Simulate matches and determine winners
        var semifinalists = new List<Tim>();
        var quarterfinalResults = new List<(Tim Winner, Tim Loser, int WinnerScore, int LoserScore)>();

        Console.WriteLine("\nQuarterfinals Results:");
        foreach (var match in quarterfinals)
        {
            var result = SimulateMatch(match.Item1, match.Item2);
            quarterfinalResults.Add(result);
            Console.WriteLine($"  {result.Winner.Team} vs {result.Loser.Team} ({result.WinnerScore}:{result.LoserScore})");
            semifinalists.Add(result.Winner);
        }

        // Simulate Semifinals
        var semifinals = new List<(Tim, Tim)>();
        var semifinalResults = new List<(Tim Winner, Tim Loser, int WinnerScore, int LoserScore)>();

        Console.WriteLine("\nSemifinals Draw:");
        var usedTeams = new HashSet<Tim>();
        while (semifinals.Count < 2 && semifinalists.Count >= 2)
        {
            Tim team1 = semifinalists.FirstOrDefault(t => !usedTeams.Contains(t));
            if (team1 != null)
            {
                usedTeams.Add(team1);
                Tim team2 = semifinalists.FirstOrDefault(t => t != team1 && !usedTeams.Contains(t));

                if (team2 != null)
                {
                    usedTeams.Add(team2);
                    semifinals.Add((team1, team2));
                    Console.WriteLine($"  {team1.Team} vs {team2.Team}");
                }
            }
        }

        Console.WriteLine("\nSemifinals Results:");
        var finalists = new List<Tim>();
        var losersForBronze = new List<Tim>();
        foreach (var match in semifinals)
        {
            var result = SimulateMatch(match.Item1, match.Item2);
            semifinalResults.Add(result);
            Console.WriteLine($"  {result.Winner.Team} vs {result.Loser.Team} ({result.WinnerScore}:{result.LoserScore})");
            finalists.Add(result.Winner);
            losersForBronze.Add(result.Loser); // Collect losers for bronze
        }

        // Ensure only two teams in finalists
        if (finalists.Count > 2)
        {
            finalists = finalists.Take(2).ToList(); // Only keep the first two teams
        }
        string thirdPlace = "";
        // Simulate the Bronze Medal Match
        Console.WriteLine("\nBronze Medal Match:");
        if (losersForBronze.Count == 2)
        {
            var bronzeMatch = losersForBronze;
            var bronzeResult = SimulateMatch(bronzeMatch[0], bronzeMatch[1]);
            Console.WriteLine($"  {bronzeResult.Winner.Team} vs {bronzeResult.Loser.Team} ({bronzeResult.WinnerScore}:{bronzeResult.LoserScore})");
            thirdPlace = bronzeResult.Winner.Team;
        }
        else
        {
            Console.WriteLine("Not enough teams available for the bronze medal match.");
        }
        string firstPlace = "";
        string secondPlace = "";
        // Simulate the Final
        Console.WriteLine("\nFinal:");
        if (finalists.Count == 2)
        {
            var finalMatch = finalists;
            var finalResult = SimulateMatch(finalMatch[0], finalMatch[1]);
            Console.WriteLine($"  {finalResult.Winner.Team} vs {finalResult.Loser.Team} ({finalResult.WinnerScore}:{finalResult.LoserScore})");
             firstPlace = finalResult.Winner.Team;
             secondPlace = finalResult.Loser.Team;
        }

        // Print Medal Winners
        Console.WriteLine("\nMedal Winners:");
        Console.WriteLine($"  1. {firstPlace} (Gold)");
        Console.WriteLine($"  2. {secondPlace} (Silver)");
        Console.WriteLine($"  3. {thirdPlace} (Bronze)");
    }







    public static void PrintTeams(List<Tim> teams)
    {
        foreach (var team in teams)
        {
            Console.WriteLine($"  {team.Team} (ISO Code: {team.ISOCode}, FIBA Ranking: {team.FIBARanking})");
        }
    }

    public static void Main(string[] args)
    {
        // Load and process group data
        var groups = LoadGroups("groups.json");
        var exhibitions = LoadExhibitions("exibitions.json");

        // Simulate the group stage
        var results = SimulateGroupStage(groups);
        var rankings = RankTeamsInGroups(groups);
        PrintGroupStageResults(results, rankings);

        // Prepare teams for the knockout stage
        var teamsForDraw = RankTeamsForDraw(rankings);

        // Perform the draw and elimination phase
        PerformDraw(teamsForDraw);
        PerformEliminationPhase(teamsForDraw);
    }
}