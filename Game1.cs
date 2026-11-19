using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CenarioBasicoXNA
{
    public class Game1 : Game
    {
        private enum EstadoCena
        {
            MundoExterno,
            InteriorCasa
        }

        private GraphicsDeviceManager _graphics;
        private BasicEffect _basicEffect;

        // Geometrias (Arrays de vértices)
        private VertexPositionColor[] _campo;
        private VertexPositionColor[] _cubo;
        private VertexPositionColor[] _piramide;
        private VertexPositionColor[] _paCatavento;

        // Tamanho do chão verde externo (80x80 => de -40 até +40 em X e Z)
        private const float LarguraChao = 80f;
        private const float ProfundidadeChao = 80f;

        // Dimensões e posições da casa e da torre no mundo externo
        private readonly Vector3 _centroCasa = new Vector3(6f, 0f, 0f);
        private readonly Vector3 _escalaBaseCasa = new Vector3(7.5f, 5.5f, 7.5f);
        private readonly Vector3 _escalaTelhadoCasa = new Vector3(8.4f, 3.6f, 8.4f);

        private readonly Vector3 _posicaoTorre = new Vector3(-8f, 0f, -2f);
        private readonly Vector3 _escalaCorpoTorre = new Vector3(4.5f, 13f, 4.5f);
        private readonly Vector3 _escalaTopoTorre = new Vector3(5.3f, 3.2f, 5.3f);

        // Dimensões e posições da sala fechada (Interior da Casa - mantido igual)
        private readonly Vector3 _centroSala = new Vector3(100f, 0f, 0f);
        private const float LarguraSala = 12f;      // X de 94 a 106
        private const float ProfundidadeSala = 12f; // Z de -6 a +6
        private const float AlturaSala = 6f;        // Y de 0 a 6

        // Pontos de entrada/saída (tecla F)
        private readonly Vector3 _pontoEntradaCasa = new Vector3(6f, 0f, 4.3f);
        private readonly Vector3 _posicaoSpawnInterior = new Vector3(100f, 2.0f, 3.8f);
        private readonly Vector3 _posicaoPortaSaida = new Vector3(100f, 0f, 5.2f);
        private readonly Vector3 _posicaoRetornoExterior = new Vector3(6f, 2.0f, 6.0f);

        // Respawn
        private const float AlturaRespawn = -30f;
        private readonly Vector3 _posicaoRespawnInicial = new Vector3(0f, 12f, 32f);

        // Estado atual da cena
        private EstadoCena _cenaAtual = EstadoCena.MundoExterno;

        // Variáveis de animação e câmera
        private float _anguloCatavento = 0f;
        private float _tempoAnimacao = 0f;
        private Matrix _view;
        private Matrix _projection;

        // Controle da câmera (WASD + Mouse livre)
        private Vector3 _posicaoCamera = new Vector3(0, 12, 32);
        private float _yaw = 0f;
        private float _pitch = -0.3f;
        private float _velocidadeMovimento = 15f;
        private float _sensibilidadeMouse = 0.003f;
        private bool _primeiroFrameMouse = true;

        // Física (Pulo, gravidade e colisão)
        private float _velocidadeY = 0f;
        private float _gravidade = 28f;
        private float _forcaPulo = 11f;
        private float _alturaCamera = 2.0f;
        private float _raioColisao = 0.7f;
        private bool _noChao = false;
        private KeyboardState _estadoTecladoAnterior;

        // Caixas de colisão por cena
        private List<BoundingBox> _obstaculosExternos = new List<BoundingBox>();
        private List<BoundingBox> _obstaculosInterior = new List<BoundingBox>();

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;
        }

        protected override void Initialize()
        {
            float aspecto = GraphicsDevice.Viewport.AspectRatio;
            _projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45f), aspecto, 0.1f, 250f);

            ConfigurarObstaculosExternos();
            ConfigurarObstaculosInterior();

            Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            _estadoTecladoAnterior = Keyboard.GetState();
            AtualizarMatrizView();

            base.Initialize();
        }

        private void ConfigurarObstaculosExternos()
        {
            _obstaculosExternos.Clear();

            // Base da casa aumentada
            float yBaseCasa = _escalaBaseCasa.Y * 0.5f;
            _obstaculosExternos.Add(CriarCaixaPorCentroEscala(
                new Vector3(_centroCasa.X, yBaseCasa, _centroCasa.Z),
                _escalaBaseCasa));

            // Telhado da casa aumentado
            float yTelhadoCasa = _escalaBaseCasa.Y + (_escalaTelhadoCasa.Y * 0.5f) - 0.1f;
            _obstaculosExternos.Add(CriarCaixaPorCentroEscala(
                new Vector3(_centroCasa.X, yTelhadoCasa, _centroCasa.Z),
                _escalaTelhadoCasa));

            // Corpo da torre/moinho aumentado
            float yCorpoTorre = _escalaCorpoTorre.Y * 0.5f;
            _obstaculosExternos.Add(CriarCaixaPorCentroEscala(
                new Vector3(_posicaoTorre.X, yCorpoTorre, _posicaoTorre.Z),
                _escalaCorpoTorre));

            // Topo da torre/moinho aumentado
            float yTopoTorre = _escalaCorpoTorre.Y + (_escalaTopoTorre.Y * 0.5f) - 0.1f;
            _obstaculosExternos.Add(CriarCaixaPorCentroEscala(
                new Vector3(_posicaoTorre.X, yTopoTorre, _posicaoTorre.Z),
                _escalaTopoTorre));
        }

        private void ConfigurarObstaculosInterior()
        {
            _obstaculosInterior.Clear();

            float cx = _centroSala.X;
            float cz = _centroSala.Z;
            float esp = 0.6f;
            float meioLarg = LarguraSala * 0.5f;
            float meioProf = ProfundidadeSala * 0.5f;

            // Parede traseira (Z negativo)
            _obstaculosInterior.Add(CriarCaixaPorCentroEscala(
                new Vector3(cx, AlturaSala * 0.5f, cz - meioProf),
                new Vector3(LarguraSala + esp, AlturaSala, esp)));

            // Parede frontal (Z positivo)
            _obstaculosInterior.Add(CriarCaixaPorCentroEscala(
                new Vector3(cx, AlturaSala * 0.5f, cz + meioProf),
                new Vector3(LarguraSala + esp, AlturaSala, esp)));

            // Parede esquerda (X negativo)
            _obstaculosInterior.Add(CriarCaixaPorCentroEscala(
                new Vector3(cx - meioLarg, AlturaSala * 0.5f, cz),
                new Vector3(esp, AlturaSala, ProfundidadeSala + esp)));

            // Parede direita (X positivo)
            _obstaculosInterior.Add(CriarCaixaPorCentroEscala(
                new Vector3(cx + meioLarg, AlturaSala * 0.5f, cz),
                new Vector3(esp, AlturaSala, ProfundidadeSala + esp)));

            // Teto da sala
            _obstaculosInterior.Add(CriarCaixaPorCentroEscala(
                new Vector3(cx, AlturaSala + 0.25f, cz),
                new Vector3(LarguraSala + esp, 0.5f, ProfundidadeSala + esp)));
        }

        private BoundingBox CriarCaixaPorCentroEscala(Vector3 centro, Vector3 escala)
        {
            Vector3 metade = escala * 0.5f;
            return new BoundingBox(centro - metade, centro + metade);
        }

        protected override void LoadContent()
        {
            _basicEffect = new BasicEffect(GraphicsDevice);
            _basicEffect.VertexColorEnabled = true;
            _basicEffect.View = _view;
            _basicEffect.Projection = _projection;

            _campo = CriarPlano(LarguraChao, ProfundidadeChao, Color.ForestGreen);
            _cubo = CriarCubo(Color.BurlyWood);
            _piramide = CriarPiramide(Color.Firebrick);
            _paCatavento = CriarCubo(Color.WhiteSmoke);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState teclado = Keyboard.GetState();

            if (teclado.IsKeyDown(Keys.Escape))
                Exit();

            if (IsActive)
            {
                float deltaTempo = (float)gameTime.ElapsedGameTime.TotalSeconds;

                _tempoAnimacao += deltaTempo;
                _anguloCatavento += 2.0f * deltaTempo;

                AtualizarCameraERotacao();
                AtualizarMovimentoEFisica(teclado, deltaTempo);
                VerificarInteracaoPorta(teclado);
                VerificarRespawn();
                AtualizarMatrizView();
            }
            else
            {
                _primeiroFrameMouse = true;
            }

            _estadoTecladoAnterior = teclado;
            base.Update(gameTime);
        }

        private void VerificarInteracaoPorta(KeyboardState teclado)
        {
            bool fPressionadoAgora = teclado.IsKeyDown(Keys.F) && !_estadoTecladoAnterior.IsKeyDown(Keys.F);
            if (!fPressionadoAgora)
                return;

            if (_cenaAtual == EstadoCena.MundoExterno)
            {
                float distCasa = Vector3.Distance(
                    new Vector3(_posicaoCamera.X, 0f, _posicaoCamera.Z),
                    new Vector3(_pontoEntradaCasa.X, 0f, _pontoEntradaCasa.Z));

                if (distCasa <= 6.0f)
                {
                    _cenaAtual = EstadoCena.InteriorCasa;
                    _posicaoCamera = _posicaoSpawnInterior;
                    _velocidadeY = 0f;
                    _noChao = false;
                    _yaw = MathHelper.Pi;
                    _pitch = 0f;
                    _primeiroFrameMouse = true;
                }
            }
            else if (_cenaAtual == EstadoCena.InteriorCasa)
            {
                float distPorta = Vector3.Distance(
                    new Vector3(_posicaoCamera.X, 0f, _posicaoCamera.Z),
                    new Vector3(_posicaoPortaSaida.X, 0f, _posicaoPortaSaida.Z));

                if (distPorta <= 3.5f)
                {
                    _cenaAtual = EstadoCena.MundoExterno;
                    _posicaoCamera = _posicaoRetornoExterior;
                    _velocidadeY = 0f;
                    _noChao = false;
                    _yaw = 0f;
                    _pitch = -0.1f;
                    _primeiroFrameMouse = true;
                }
            }
        }

        private void VerificarRespawn()
        {
            if (_posicaoCamera.Y < AlturaRespawn)
            {
                if (_cenaAtual == EstadoCena.MundoExterno)
                {
                    _posicaoCamera = _posicaoRespawnInicial;
                    _yaw = 0f;
                    _pitch = -0.3f;
                }
                else
                {
                    _posicaoCamera = _posicaoSpawnInterior;
                    _yaw = MathHelper.Pi;
                    _pitch = 0f;
                }

                _velocidadeY = 0f;
                _noChao = false;
                _primeiroFrameMouse = true;
            }
        }

        private void AtualizarCameraERotacao()
        {
            int centroX = GraphicsDevice.Viewport.Width / 2;
            int centroY = GraphicsDevice.Viewport.Height / 2;
            MouseState mouse = Mouse.GetState();

            if (_primeiroFrameMouse)
            {
                Mouse.SetPosition(centroX, centroY);
                _primeiroFrameMouse = false;
            }
            else
            {
                float deltaX = mouse.X - centroX;
                float deltaY = mouse.Y - centroY;

                _yaw += deltaX * _sensibilidadeMouse;
                _pitch -= deltaY * _sensibilidadeMouse;

                float limitePitch = MathHelper.PiOver2 - 0.05f;
                _pitch = MathHelper.Clamp(_pitch, -limitePitch, limitePitch);

                Mouse.SetPosition(centroX, centroY);
            }
        }

        private void AtualizarMovimentoEFisica(KeyboardState teclado, float deltaTempo)
        {
            Vector3 direcaoFrente = new Vector3(
                (float)(Math.Cos(_pitch) * Math.Sin(_yaw)),
                (float)Math.Sin(_pitch),
                (float)(-Math.Cos(_pitch) * Math.Cos(_yaw))
            );
            direcaoFrente.Normalize();

            Vector3 frentePlano = new Vector3(direcaoFrente.X, 0f, direcaoFrente.Z);
            if (frentePlano != Vector3.Zero)
                frentePlano.Normalize();

            Vector3 direcaoDireita = Vector3.Cross(frentePlano, Vector3.Up);
            if (direcaoDireita != Vector3.Zero)
                direcaoDireita.Normalize();

            Vector3 movimentoHorizontal = Vector3.Zero;
            if (teclado.IsKeyDown(Keys.W)) movimentoHorizontal += frentePlano;
            if (teclado.IsKeyDown(Keys.S)) movimentoHorizontal -= frentePlano;
            if (teclado.IsKeyDown(Keys.D)) movimentoHorizontal += direcaoDireita;
            if (teclado.IsKeyDown(Keys.A)) movimentoHorizontal -= direcaoDireita;

            if (movimentoHorizontal != Vector3.Zero)
            {
                movimentoHorizontal.Normalize();
                _posicaoCamera.X += movimentoHorizontal.X * _velocidadeMovimento * deltaTempo;
                _posicaoCamera.Z += movimentoHorizontal.Z * _velocidadeMovimento * deltaTempo;
                ResolverColisaoHorizontal();
            }

            bool espacoPressionadoAgora = teclado.IsKeyDown(Keys.Space) && !_estadoTecladoAnterior.IsKeyDown(Keys.Space);
            if (espacoPressionadoAgora && _noChao)
            {
                _velocidadeY = _forcaPulo;
                _noChao = false;
            }

            _velocidadeY -= _gravidade * deltaTempo;
            _posicaoCamera.Y += _velocidadeY * deltaTempo;
            _noChao = false;

            ResolverColisaoVertical();
        }

        private List<BoundingBox> ObstaculosAtuais()
        {
            return _cenaAtual == EstadoCena.MundoExterno ? _obstaculosExternos : _obstaculosInterior;
        }

        private bool EstaSobreChaoAtual()
        {
            if (_cenaAtual == EstadoCena.MundoExterno)
            {
                float metadeLargura = LarguraChao * 0.5f;
                float metadeProfundidade = ProfundidadeChao * 0.5f;

                return _posicaoCamera.X >= -metadeLargura &&
                       _posicaoCamera.X <= metadeLargura &&
                       _posicaoCamera.Z >= -metadeProfundidade &&
                       _posicaoCamera.Z <= metadeProfundidade;
            }
            else
            {
                float metadeLarg = LarguraSala * 0.5f;
                float metadeProf = ProfundidadeSala * 0.5f;

                return _posicaoCamera.X >= _centroSala.X - metadeLarg &&
                       _posicaoCamera.X <= _centroSala.X + metadeLarg &&
                       _posicaoCamera.Z >= _centroSala.Z - metadeProf &&
                       _posicaoCamera.Z <= _centroSala.Z + metadeProf;
            }
        }

        private void ResolverColisaoHorizontal()
        {
            float baseY = _posicaoCamera.Y - _alturaCamera;
            float topoY = _posicaoCamera.Y + 0.2f;

            foreach (BoundingBox caixa in ObstaculosAtuais())
            {
                bool sobrepoeY = topoY > caixa.Min.Y && baseY < caixa.Max.Y;
                if (!sobrepoeY)
                    continue;

                float maisProximoX = MathHelper.Clamp(_posicaoCamera.X, caixa.Min.X, caixa.Max.X);
                float maisProximoZ = MathHelper.Clamp(_posicaoCamera.Z, caixa.Min.Z, caixa.Max.Z);

                float dx = _posicaoCamera.X - maisProximoX;
                float dz = _posicaoCamera.Z - maisProximoZ;
                float distSq = dx * dx + dz * dz;

                if (distSq < _raioColisao * _raioColisao)
                {
                    float dist = (float)Math.Sqrt(distSq);
                    if (dist > 0.0001f)
                    {
                        float empurrao = _raioColisao - dist;
                        _posicaoCamera.X += (dx / dist) * empurrao;
                        _posicaoCamera.Z += (dz / dist) * empurrao;
                    }
                    else
                    {
                        float penEsq = Math.Abs(_posicaoCamera.X - caixa.Min.X);
                        float penDir = Math.Abs(caixa.Max.X - _posicaoCamera.X);
                        float penFrente = Math.Abs(caixa.Max.Z - _posicaoCamera.Z);
                        float penTras = Math.Abs(_posicaoCamera.Z - caixa.Min.Z);

                        float minPen = Math.Min(Math.Min(penEsq, penDir), Math.Min(penFrente, penTras));
                        if (minPen == penEsq) _posicaoCamera.X = caixa.Min.X - _raioColisao;
                        else if (minPen == penDir) _posicaoCamera.X = caixa.Max.X + _raioColisao;
                        else if (minPen == penFrente) _posicaoCamera.Z = caixa.Max.Z + _raioColisao;
                        else _posicaoCamera.Z = caixa.Min.Z - _raioColisao;
                    }
                }
            }
        }

        private void ResolverColisaoVertical()
        {
            float pesY = _posicaoCamera.Y - _alturaCamera;
            float cabecaY = _posicaoCamera.Y + 0.2f;

            foreach (BoundingBox caixa in ObstaculosAtuais())
            {
                bool dentroXZ =
                    _posicaoCamera.X + _raioColisao > caixa.Min.X &&
                    _posicaoCamera.X - _raioColisao < caixa.Max.X &&
                    _posicaoCamera.Z + _raioColisao > caixa.Min.Z &&
                    _posicaoCamera.Z - _raioColisao < caixa.Max.Z;

                if (!dentroXZ)
                    continue;

                if (_velocidadeY <= 0f && pesY <= caixa.Max.Y && cabecaY > caixa.Max.Y)
                {
                    _posicaoCamera.Y = caixa.Max.Y + _alturaCamera;
                    _velocidadeY = 0f;
                    _noChao = true;
                    pesY = _posicaoCamera.Y - _alturaCamera;
                }
                else if (_velocidadeY > 0f && cabecaY >= caixa.Min.Y && pesY < caixa.Min.Y)
                {
                    _posicaoCamera.Y = caixa.Min.Y - 0.2f;
                    _velocidadeY = 0f;
                }
            }

            if (EstaSobreChaoAtual())
            {
                float alturaMinimaChao = _alturaCamera;
                if (_posicaoCamera.Y <= alturaMinimaChao)
                {
                    _posicaoCamera.Y = alturaMinimaChao;
                    _velocidadeY = 0f;
                    _noChao = true;
                }
            }
        }

        private void AtualizarMatrizView()
        {
            Vector3 direcaoFrente = new Vector3(
                (float)(Math.Cos(_pitch) * Math.Sin(_yaw)),
                (float)Math.Sin(_pitch),
                (float)(-Math.Cos(_pitch) * Math.Cos(_yaw))
            );

            _view = Matrix.CreateLookAt(_posicaoCamera, _posicaoCamera + direcaoFrente, Vector3.Up);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            _basicEffect.View = _view;

            if (_cenaAtual == EstadoCena.MundoExterno)
            {
                DesenharMundoExterno();
            }
            else
            {
                DesenharInteriorCasa();
            }

            base.Draw(gameTime);
        }

        private void DesenharMundoExterno()
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Campo verde
            DesenharMalha(_campo, Matrix.Identity);

            // Casinha maior (à direita)
            float yBaseCasa = _escalaBaseCasa.Y * 0.5f;
            Matrix matrizBaseCasa = Matrix.CreateScale(_escalaBaseCasa) *
                                    Matrix.CreateTranslation(_centroCasa.X, yBaseCasa, _centroCasa.Z);
            DesenharMalha(_cubo, matrizBaseCasa);

            // Porta na frente da casinha + maçaneta dourada
            float frenteCasaZ = _centroCasa.Z + (_escalaBaseCasa.Z * 0.5f) + 0.08f;
            DesenharMalha(
                CriarCubo(new Color(92, 48, 22)),
                Matrix.CreateScale(1.8f, 3.2f, 0.25f) * Matrix.CreateTranslation(_centroCasa.X, 1.6f, frenteCasaZ));

            DesenharMalha(
                CriarCubo(Color.Gold),
                Matrix.CreateScale(0.18f, 0.18f, 0.2f) * Matrix.CreateTranslation(_centroCasa.X + 0.62f, 1.55f, frenteCasaZ + 0.16f));

            // Telhado da casinha
            float yTelhadoCasa = _escalaBaseCasa.Y + (_escalaTelhadoCasa.Y * 0.5f) - 0.1f;
            Matrix matrizTelhadoCasa = Matrix.CreateScale(_escalaTelhadoCasa) *
                                       Matrix.CreateTranslation(_centroCasa.X, yTelhadoCasa, _centroCasa.Z);
            DesenharMalha(_piramide, matrizTelhadoCasa);

            // Torre / moinho maior (à esquerda)
            float yCorpoTorre = _escalaCorpoTorre.Y * 0.5f;
            Matrix matrizCorpoTorre = Matrix.CreateScale(_escalaCorpoTorre) *
                                      Matrix.CreateTranslation(_posicaoTorre.X, yCorpoTorre, _posicaoTorre.Z);
            DesenharMalha(_cubo, matrizCorpoTorre);

            float yTopoTorre = _escalaCorpoTorre.Y + (_escalaTopoTorre.Y * 0.5f) - 0.1f;
            Matrix matrizTopoTorre = Matrix.CreateScale(_escalaTopoTorre) *
                                     Matrix.CreateTranslation(_posicaoTorre.X, yTopoTorre, _posicaoTorre.Z);
            DesenharMalha(_piramide, matrizTopoTorre);

            // Pás do catavento proporcionais ao tamanho do moinho
            float frenteTorreZ = _posicaoTorre.Z + (_escalaCorpoTorre.Z * 0.5f) + 0.2f;
            Vector3 eixoCatavento = new Vector3(_posicaoTorre.X, _escalaCorpoTorre.Y * 0.85f, frenteTorreZ);

            Matrix pa1 = Matrix.CreateScale(0.65f, 7.5f, 0.16f) *
                         Matrix.CreateRotationZ(_anguloCatavento) *
                         Matrix.CreateTranslation(eixoCatavento);
            DesenharMalha(_paCatavento, pa1);

            Matrix pa2 = Matrix.CreateScale(0.65f, 7.5f, 0.16f) *
                         Matrix.CreateRotationZ(_anguloCatavento + MathHelper.PiOver2) *
                         Matrix.CreateTranslation(eixoCatavento);
            DesenharMalha(_paCatavento, pa2);
        }

        private void DesenharInteriorCasa()
        {
            GraphicsDevice.Clear(new Color(18, 16, 24));

            float cx = _centroSala.X;
            float cz = _centroSala.Z;
            float esp = 0.6f;
            float meioLarg = LarguraSala * 0.5f;
            float meioProf = ProfundidadeSala * 0.5f;

            // Chão de madeira da sala
            DesenharMalha(
                CriarPlano(LarguraSala, ProfundidadeSala, Color.SaddleBrown),
                Matrix.CreateTranslation(cx, 0f, cz));

            // 4 paredes e teto
            DesenharMalha(_cubo, Matrix.CreateScale(LarguraSala + esp, AlturaSala, esp) * Matrix.CreateTranslation(cx, AlturaSala * 0.5f, cz - meioProf));
            DesenharMalha(_cubo, Matrix.CreateScale(LarguraSala + esp, AlturaSala, esp) * Matrix.CreateTranslation(cx, AlturaSala * 0.5f, cz + meioProf));
            DesenharMalha(_cubo, Matrix.CreateScale(esp, AlturaSala, ProfundidadeSala + esp) * Matrix.CreateTranslation(cx - meioLarg, AlturaSala * 0.5f, cz));
            DesenharMalha(_cubo, Matrix.CreateScale(esp, AlturaSala, ProfundidadeSala + esp) * Matrix.CreateTranslation(cx + meioLarg, AlturaSala * 0.5f, cz));
            DesenharMalha(_cubo, Matrix.CreateScale(LarguraSala + esp, 0.5f, ProfundidadeSala + esp) * Matrix.CreateTranslation(cx, AlturaSala + 0.25f, cz));

            // Porta de saída na parede frontal
            DesenharMalha(
                CriarCubo(new Color(92, 48, 22)),
                Matrix.CreateScale(2.0f, 3.2f, 0.35f) * Matrix.CreateTranslation(cx, 1.6f, cz + meioProf - 0.35f));

            DesenharMalha(
                CriarCubo(Color.Gold),
                Matrix.CreateScale(0.18f, 0.18f, 0.25f) * Matrix.CreateTranslation(cx + 0.65f, 1.5f, cz + meioProf - 0.55f));

            // Tochas nas paredes
            DesenharTocha(new Vector3(cx - meioLarg + 0.55f, 2.5f, cz - 2.5f));
            DesenharTocha(new Vector3(cx + meioLarg - 0.55f, 2.5f, cz - 2.5f));
            DesenharTocha(new Vector3(cx - meioLarg + 0.55f, 2.5f, cz + 2.5f));
            DesenharTocha(new Vector3(cx + meioLarg - 0.55f, 2.5f, cz + 2.5f));
        }

        private void DesenharTocha(Vector3 posicaoBase)
        {
            DesenharMalha(
                CriarCubo(Color.DarkGoldenrod),
                Matrix.CreateScale(0.18f, 0.9f, 0.18f) * Matrix.CreateTranslation(posicaoBase));

            float oscilacao = 1f + 0.12f * (float)Math.Sin(_tempoAnimacao * 12f + posicaoBase.Z);
            Vector3 posChama = posicaoBase + new Vector3(0f, 0.65f, 0f);

            DesenharMalha(
                CriarPiramide(Color.OrangeRed),
                Matrix.CreateScale(0.32f * oscilacao, 0.55f * oscilacao, 0.32f * oscilacao) * Matrix.CreateTranslation(posChama));
        }

        private void DesenharMalha(VertexPositionColor[] vertices, Matrix world)
        {
            _basicEffect.World = world;
            foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3);
            }
        }

        #region Geradores de Formas 3D (Primitivas)

        private VertexPositionColor[] CriarPlano(float largura, float profundidade, Color cor)
        {
            float x = largura / 2f;
            float z = profundidade / 2f;

            Vector3 p1 = new Vector3(-x, 0, -z);
            Vector3 p2 = new Vector3(x, 0, -z);
            Vector3 p3 = new Vector3(-x, 0, z);
            Vector3 p4 = new Vector3(x, 0, z);

            return new VertexPositionColor[]
            {
                new VertexPositionColor(p1, cor),
                new VertexPositionColor(p2, cor),
                new VertexPositionColor(p3, cor),
                new VertexPositionColor(p2, cor),
                new VertexPositionColor(p4, cor),
                new VertexPositionColor(p3, cor)
            };
        }

        private VertexPositionColor[] CriarCubo(Color corBase)
        {
            Color cFrente = corBase;
            Color cLado = Color.Lerp(corBase, Color.Black, 0.2f);
            Color cTopo = Color.Lerp(corBase, Color.White, 0.15f);

            Vector3[] v = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f,  0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f)
            };

            List<VertexPositionColor> lista = new List<VertexPositionColor>();

            AdicionarFace(lista, v[0], v[2], v[3], v[1], cFrente);
            AdicionarFace(lista, v[5], v[7], v[6], v[4], cFrente);
            AdicionarFace(lista, v[4], v[6], v[2], v[0], cLado);
            AdicionarFace(lista, v[1], v[3], v[7], v[5], cLado);
            AdicionarFace(lista, v[2], v[6], v[7], v[3], cTopo);
            AdicionarFace(lista, v[4], v[0], v[1], v[5], cLado);

            return lista.ToArray();
        }

        private VertexPositionColor[] CriarPiramide(Color corBase)
        {
            Color cFrente = corBase;
            Color cLado = Color.Lerp(corBase, Color.Black, 0.25f);

            Vector3 topo = new Vector3(0f, 0.5f, 0f);
            Vector3 fEsq = new Vector3(-0.5f, -0.5f, 0.5f);
            Vector3 fDir = new Vector3(0.5f, -0.5f, 0.5f);
            Vector3 tEsq = new Vector3(-0.5f, -0.5f, -0.5f);
            Vector3 tDir = new Vector3(0.5f, -0.5f, -0.5f);

            List<VertexPositionColor> lista = new List<VertexPositionColor>();

            lista.Add(new VertexPositionColor(fEsq, cFrente)); lista.Add(new VertexPositionColor(topo, cFrente)); lista.Add(new VertexPositionColor(fDir, cFrente));
            lista.Add(new VertexPositionColor(fDir, cLado));   lista.Add(new VertexPositionColor(topo, cLado));   lista.Add(new VertexPositionColor(tDir, cLado));
            lista.Add(new VertexPositionColor(tDir, cFrente)); lista.Add(new VertexPositionColor(topo, cFrente)); lista.Add(new VertexPositionColor(tEsq, cFrente));
            lista.Add(new VertexPositionColor(tEsq, cLado));   lista.Add(new VertexPositionColor(topo, cLado));   lista.Add(new VertexPositionColor(fEsq, cLado));

            AdicionarFace(lista, tEsq, fEsq, fDir, tDir, cLado);

            return lista.ToArray();
        }

        private void AdicionarFace(List<VertexPositionColor> lista, Vector3 infEsq, Vector3 supEsq, Vector3 supDir, Vector3 infDir, Color cor)
        {
            lista.Add(new VertexPositionColor(infEsq, cor));
            lista.Add(new VertexPositionColor(supEsq, cor));
            lista.Add(new VertexPositionColor(supDir, cor));

            lista.Add(new VertexPositionColor(infEsq, cor));
            lista.Add(new VertexPositionColor(supDir, cor));
            lista.Add(new VertexPositionColor(infDir, cor));
        }

        #endregion
    }
}