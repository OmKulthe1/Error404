using GLTFast.Schema;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{

    public string sceneName1;
    public string sceneName2;

    public void SceneChange1() { 
        SceneManager.LoadScene(sceneName1);
    }

    public void SceneChange2() { 
        SceneManager.LoadScene(sceneName2);
    }
}
