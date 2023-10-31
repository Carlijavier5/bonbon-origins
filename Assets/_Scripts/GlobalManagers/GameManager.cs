using UnityEngine;

/// <summary>
/// Singleton class that holds persistent data and handles transitions;
/// </summary>
public partial class GameManager : MonoBehaviour {

    [SerializeField] private int[] levelIndeces;

    /// <summary> Internal reference to the active GameManager; </summary>
    private static GameManager _instance;
    /// <summary> Public instance reference to the active GameManager; </summary>
    public static GameManager Instance => _instance;

    /// <summary> Index of the level scene currently loaded; </summary>
    public int CurrLevel { get {
            if (levelIndeces == null) return 0;
            else {
                int sceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
                int currLevel = 0;
                foreach (int lvlIndex in levelIndeces) {
                    if (lvlIndex <= sceneIndex) currLevel++;
                    else break;
                } return currLevel;
            }
        }
    }

    void Awake() {
        /// Initialize Singleton;
        if (_instance != null) {
            Destroy(gameObject);
        } else {
            DontDestroyOnLoad(gameObject);
            _instance = this;
        }
    }
}