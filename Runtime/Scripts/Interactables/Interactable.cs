using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Twinny.XR.Interactables
{
    public class Interactable : MonoBehaviour
    {
        #region Fields
        [SerializeField] private MeshRenderer[] _highLightMeshes;
        [ColorUsage(true, true)]
        [SerializeField] private Color _normalColor; // Cor de destaque
        [ColorUsage(true, true)]
        [SerializeField] private Color _highLightColor; // Cor de destaque
        [SerializeField] private float _transitionTime = 2f; // Tempo de transição

        private float _transitionProgress = 0f; // Progresso da transição (0 a 1)
        [SerializeField]
        private bool _isHighLight = false; // Flag de highlight
        #endregion

        // Start is called before the first frame update
        void Start()
        {
            // Garante que os materiais tenham a palavra-chave "_EMISSION" ativada
            foreach (var mesh in _highLightMeshes)
            {
                if (mesh != null)
                {
                    mesh.material.EnableKeyword("_EMISSION");
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            // Se o Highlight está ativado
            if (_isHighLight)
            {
                // Aumenta o progresso para a cor emissiva
                _transitionProgress += Time.deltaTime / _transitionTime;
            }
            else
            {
                // Diminui o progresso de volta para a cor normal
                _transitionProgress -= Time.deltaTime / _transitionTime;
            }

            // Limita o progresso entre 0 e 1
            _transitionProgress = Mathf.Clamp01(_transitionProgress);

            // Aplica a interpolação de cor
            for (int i = 0; i < _highLightMeshes.Length; i++)
            {
                if (_highLightMeshes[i] != null)
                {
                    // Se for Highlight, vai para a cor emissiva, senão volta para a cor padrão (sem emissão)
                    Color targetColor = _isHighLight ? _highLightColor : _normalColor;

                    // Realiza a interpolação suave para a cor de emissão
                    Color newEmissionColor = Color.Lerp(_highLightMeshes[i].material.GetColor("_EmissionColor"), targetColor, _transitionProgress);
                    _highLightMeshes[i].material.SetColor("_EmissionColor", newEmissionColor);

                    // Desativa a emissão caso a cor de transição esteja de volta ao normal
                    if (_transitionProgress == 0f)
                    {
                        _highLightMeshes[i].material.DisableKeyword("_EMISSION");
                    }
                    else
                    {
                        _highLightMeshes[i].material.EnableKeyword("_EMISSION");
                    }
                }
            }
        }

        // Função para alternar o Highlight via Menu de Contexto no Inspector
        [ContextMenu("HIGHLIGHT")]
        private void SetHighLight2()
        {
            SetHighLight(!_isHighLight);
        }

        // Função para ativar ou desativar o highlight
        public void SetHighLight(bool active = true)
        {
           // _transitionProgress = active ? _transitionProgress : 1f; // Se for falso, começa do último valor de transição
            _isHighLight = active;
        }
    }
}
