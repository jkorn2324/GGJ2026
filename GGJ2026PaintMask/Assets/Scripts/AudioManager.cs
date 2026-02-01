using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public AudioClip SFX_click;
    public AudioClip SFX_paint;
    public AudioClip SFX_tape_apply;
    public AudioClip SFX_tape_remove;
    public AudioClip SFX_timer;
    public float SFX_click_volume = 1f;
    public float SFX_paint_volume = 1f;
    public float SFX_tape_apply_volume = 1f;
    public float SFX_tape_remove_volume = 1f;
    public float SFX_timer_volume = 1f;
    
    [SerializeField]
    private AudioSource source;
    
    private void Awake()
    {
        if (!source)
        {
            source = GetComponent<AudioSource>();
        }
    }

    public void PlayClickSFX()
    {
        source.PlayOneShot(SFX_click, SFX_click_volume);
    }
    public void PlayPaintSFX()
    {
        source.PlayOneShot(SFX_paint, SFX_paint_volume);
    }
    public void PlayTapeApplySFX()
    {
        source.PlayOneShot(SFX_tape_apply, SFX_tape_apply_volume);
    }
    public void PlayTapeRemoveSFX()
    {
        source.PlayOneShot(SFX_tape_remove, SFX_tape_remove_volume);
    }
    public void PlayTimerSFX()
    {
        source.PlayOneShot(SFX_timer, SFX_timer_volume);
    }
}
