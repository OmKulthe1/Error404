using GLTFast.Schema;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    public void SceneChange() { 
        SceneManager.LoadScene("TheurTempleScene");
    }
}
