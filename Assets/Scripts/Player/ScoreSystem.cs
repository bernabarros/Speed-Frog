using UnityEngine;
using TMPro;

public class ScoreSystem : MonoBehaviour
{
    private int score;
    [SerializeField] private TextMeshProUGUI scoreText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scoreText.text = score.ToString("D5");
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Coin")
        {
            Destroy(collision.gameObject);
            score += 10;
            scoreText.text = score.ToString("D5");
        }
    }
}
