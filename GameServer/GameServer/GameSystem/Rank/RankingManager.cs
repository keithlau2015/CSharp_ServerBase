using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class RankingManager : Singleton<RankingManager>
{
    public Dictionary<int, List<string>> rankingMap = new Dictionary<int, List<string>>();
    public event Action onRankChange;

    public void UpdateRankingInstance(int rankID)
    {
        List<string> rankingList = null;
        if(!rankingMap.TryGetValue(rankID, out rankingList))
        {
            Debug.DebugUtility.WarningLog($"Update Ranking Instance Failed: Invaild rankID [{rankID}]");
            return;
        }


    }
}