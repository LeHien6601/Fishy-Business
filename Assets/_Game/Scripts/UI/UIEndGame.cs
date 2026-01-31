using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using HHDCore;
using TMPro;
using UnityEngine;

public class UIEndGame : UIView
{
    #region Properties 
    [Header("References")]
    [SerializeField] private CustomLayout _layout;
    [SerializeField] private List<UIPlayerEndGame> _uiPlayers = new();
    [SerializeField] private TextMeshProUGUI _loseTMP;

    [Header("VFX")]
    [SerializeField] private Transform _fireWorkTransform;
    [SerializeField] private Transform _sparksTransform;
    [SerializeField] private ParticleSystem _fireWork;
    [SerializeField] private ParticleSystem _sparks;

    #endregion

    #region View behavior
    public override async void Show()
    {
        UpdateData(GameplayManager.Instance.GetWinnerInfos());
        base.Show();
        SpawnVFX();
        await Task.Delay((int)(1000 * Constant.RESTART_INTERVAL));
        Hide();
        UIManager.Instance.HideUI(EUIState.InGame);
    }

    public override void Hide()
    {
        base.Hide();
        _fireWorkTransform.DeleteChildren();
        _sparksTransform.DeleteChildren();
    }
    #endregion

    #region Setter
    public async void UpdateData(List<LobbyManager.PlayerInfo> playerInfos)
    {
        _loseTMP.gameObject.SetActive(false);
        for (int i = 0; i < _uiPlayers.Count; i++)
        {
            if (i < playerInfos.Count)
            {
                _uiPlayers[i].UpdatePlayerInfo(playerInfos[i].Name, playerInfos[i].IconId);
                _uiPlayers[i].gameObject.SetActive(true);
            }
            else
            {
                _uiPlayers[i].gameObject.SetActive(false);
            }
        }
        _layout.UpdateLayoutFitType();
        if (playerInfos.Count == 0)
        {
            SoundManager.Play2D(SoundType.Lose);
            _loseTMP.gameObject.SetActive(true);
            _loseTMP.rectTransform.localScale = Vector3.zero;
            _loseTMP.rectTransform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            _loseTMP.rectTransform.DOShakePosition(2f, 10f, 20, 90f, false, true);
            await Task.Delay(2500);
            _loseTMP.rectTransform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
        }
        else
        {
            SoundManager.Play2D(SoundType.Victory);
        }
    }
    #endregion

    #region VFX

    private void SpawnVFX()
    {
        SpawnSparks();
        StartCoroutine(SpawnFireworks(10));
    }

    private IEnumerator SpawnFireworks(int count)
    {
        var waitToSpawn = new WaitForSecondsRealtime(0.5f);
        for (int i = 0; i < count; i++)
        {
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-800f, 800f),
                UnityEngine.Random.Range(-350f, 350f),
                0f
            );

            Vector3 spawnPos = _fireWork.transform.localPosition + randomOffset;
            var firework = Instantiate(_fireWork, _fireWorkTransform);
            firework.transform.localPosition = _fireWork.transform.localPosition + randomOffset;
            firework.transform.localRotation = _fireWork.transform.localRotation;
            firework.Play();
            yield return waitToSpawn;
        }
    }

    private void SpawnSparks()
    {
        var sparks = Instantiate(_sparks, _sparksTransform);
        sparks.transform.localPosition = Vector3.zero;
        sparks.transform.localRotation = _sparks.transform.localRotation;
        sparks.Play();
    }
    #endregion
}
