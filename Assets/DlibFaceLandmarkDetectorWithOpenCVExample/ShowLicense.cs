using UnityEngine;
using UnityEngine.SceneManagement;

namespace DlibFaceLandmarkDetectorWithOpenCVExample
{
    /// <summary>
    /// Show License
    /// A scene that displays the DlibFaceLandmarkDetector license text.
    /// </summary>
    public class ShowLicense : MonoBehaviour
    {
        // Unity Lifecycle Methods
        private void Start()
        {

        }

        private void Update()
        {

        }

        // Public Methods
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("DlibFaceLandmarkDetectorExample");
        }
    }
}
