using UnityEngine;

public class PlaySFX : MonoBehaviour
{
    [SerializeField] AudioClip[] audioClips;
    [SerializeField] float minRandomTime;
    [SerializeField] float maxRandomTime;
    AudioSource audioSource;
    float time = 0;
    float randomTime = 0;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        randomTime = Random.Range(minRandomTime, maxRandomTime);
    }

    void Update()
    {
        time += Time.deltaTime;

        if (time > randomTime)
        {
            audioSource.PlayOneShot(audioClips[Random.Range(0, audioClips.Length)]);
            time = 0;
        }
    }


}
