using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

using SeventyOneSquared;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;

namespace SeventyOneSquared
{
	[CustomEditor(typeof(PDUnity))]
	[CanEditMultipleObjects]
	public class PDUnityEditor : Editor 
	{
		public override void OnInspectorGUI()
		{
			PDUnity pdUnity = (PDUnity)target;

			pdUnity.EmitterFile = (TextAsset)EditorGUILayout.ObjectField(new GUIContent("Emitter File"), pdUnity.EmitterFile, typeof(TextAsset), false, GUILayout.Height(18));
			pdUnity.emitterOrigin = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Emitter Origin"), pdUnity.emitterOrigin, typeof(GameObject), true, GUILayout.Height(18));
			pdUnity.scale = EditorGUILayout.Slider("Scale", pdUnity.scale, 0.001f, 0.1f, GUILayout.Height (18));
			
			Rect resetButtonRect = GUILayoutUtility.GetRect (18, 18, "TextField");
			if (GUI.Button (resetButtonRect, "Restart")) {
				pdUnity.reset();
			}
			
			pdUnity.Running = EditorGUILayout.Toggle("Running", pdUnity.Running, GUILayout.Height(18));
			pdUnity.AutoLoop = EditorGUILayout.Toggle("AutoLoop", pdUnity.AutoLoop, GUILayout.Height(18));
			pdUnity.RunInEditor = EditorGUILayout.Toggle("Run in Editor", pdUnity.RunInEditor, GUILayout.Height(18));
			
			string[] BlendModeNames = new string[]
			{
				"Inherit",
				"Zero", 
				"One", 
				"DstColor", 
				"SrcColor", 
				"OneMinusDstColor", 
				"SrcAlpha", 
				"OneMinusSrcColor", 
				"DstAlpha", 
				"OneMinusDstAlpha", 
				"SrcAlphaSaturate", 
				"OneMinusSrcAlpha",
			};
			pdUnity.blendSource = EditorGUILayout.Popup("Blend Source", pdUnity.blendSource, BlendModeNames);
			pdUnity.blendDestination = EditorGUILayout.Popup("Blend Destination", pdUnity.blendDestination, BlendModeNames);
			pdUnity.texture = (Texture)EditorGUILayout.ObjectField(new GUIContent("Texture"), pdUnity.texture, typeof(Texture), false, GUILayout.Height(64));
		}
	}
}

#endif

namespace SeventyOneSquared
{
	[ExecuteInEditMode]
	[Serializable]
	public class PDUnity : MonoBehaviour {
		
		private class PDUnityParticleEmitter
		{
			public string textureName;
			public byte[] textureData;
			public Texture2D texture = null;

			public Int32 positionType;
			public Int32 yScale;

			public bool opacityModifyRGB;
			public Int32 blendFuncSource;
			public Int32 blendFuncDestination;

			public Vector2 sourcePosition;
			public Vector2 sourcePositionVariance;
			public float speed;
			public float speedVariance;
			public float lifeSpan;
			public float lifeSpanVariance;
			public float angle;
			public float angleVariance;
			public Vector2 gravity;
			
			public float radialAcceleration;
			public float radialAccelerationVariance;
			public float tangentialAcceleration;
			public float tangentialAccelerationVariance;
			
			public Color startColor;
			public Color startColorVariance;
			public Color finishColor;
			public Color finishColorVariance;
			
			public Int32 maxParticles;
			
			public float startParticleSize;
			public float startParticleSizeVariance;
			public float finishParticleSize;
			public float finishParticleSizeVariance;
			
			public float duration;
			public kParticleTypes emitterType;
			
			public float minRadius;
			public float minRadiusVariance;
			public float maxRadius;
			public float maxRadiusVariance;
			
			public float rotatePerSecond;
			public float rotatePerSecondVariance;
			

			public float rotationStart;
			public float rotationStartVariance;
			public float rotationEnd;
			public float rotationEndVariance;
			
			public float emissionRate;
			public float emitCounter;
			public float elapsedTime;
			
			public int particleCount;
		}
		
		// Particle type
		private enum kParticleTypes
		{
			kParticleTypeGravity,
			kParticleTypeRadial
		};
		
		// Structure used to hold particle specific information
		private struct Particle
		{
			public Vector2 position;
			public Vector2 direction;
			public Vector2 startPos;
			public Color color;
			public Color deltaColor;
			public float rotation;
			public float rotationDelta;
			public float radialAcceleration;
			public float tangentialAcceleration;
			public float radius;
			public float radiusDelta;
			public float angle;
			public float degreesPerSecond;
			public float particleSize;
			public float particleSizeDelta;
			public float timeToLive;
		};
		
		// Visible In Editor
		public TextAsset EmitterFile;
		public Texture texture;
		public GameObject emitterOrigin = null;
		public bool Running = true;
		public bool AutoLoop = true;
		public bool RunInEditor = true;
		public float scale = .04f;
		
		public int blendSource = 0;
		public int blendDestination = 0;


		// Private
		private TextAsset currentEmitterFile;
		private Texture currentTexture;
		private int currentBlendSource = 0;
		private int currentBlendDestination = 0;
		private PDUnityParticleEmitter emitterConfig;
		
		// Components
		// Creates a material that is explicitly created & destroyed by the component.
		// Resources.UnloadUnusedAssets will not unload it, and it will not be editable by the inspector.
		private Material material = null;
		private Mesh mesh = null;
		private MeshFilter meshFilter = null;
		private MeshRenderer meshRenderer = null;
		
		private Particle[] particles = new Particle[2000];
		
		private bool initialized = false;
		void Initialize()
		{
			if (this.initialized) {
				return;
			}

			//========================================================
			// MESH FILTER
			//========================================================
			if (gameObject.GetComponent<MeshFilter>() == null)
			{
				meshFilter = gameObject.AddComponent<MeshFilter>();
			}
			
			meshFilter = GetComponent<MeshFilter>();
			meshFilter.hideFlags = HideFlags.HideInInspector;
			
			
			//========================================================
			// MESH
			//========================================================
			
			if (Application.isEditor) {
				mesh = meshFilter.sharedMesh;
			
				if (mesh == null) {
					mesh = new Mesh ();
				}
			
				meshFilter.sharedMesh = mesh;
			
			} else {
				mesh = meshFilter.mesh;
			
				if (mesh == null) {
					mesh = new Mesh ();
				}
			
				meshFilter.mesh = mesh;
			}
			
			mesh.MarkDynamic();
			
			//========================================================
			// MESH RENDERER
			//========================================================
			if (gameObject.GetComponent<MeshRenderer>() == null)
			{
				gameObject.AddComponent<MeshRenderer>();
			}
			
			meshRenderer = GetComponent<MeshRenderer>();
			meshRenderer.hideFlags = HideFlags.HideInInspector;
			
			
			//========================================================
			// SHADER
			//========================================================
			Shader shader = Shader.Find("PDUnityShader");


			//========================================================
			// MATERIAL
			//========================================================
			if (Application.isEditor) {
				material = meshRenderer.sharedMaterial;
				if (material == null) {
					material = new Material (shader);
					material.hideFlags = HideFlags.HideAndDontSave;
				} else {
					material.shader = shader;
				}
				meshRenderer.sharedMaterial = material;
			} else {
				material = new Material (shader);
				meshRenderer.material = material;
			}

			this.initialized = true;
		}
		
		private void loadTexture()
		{
			Texture newTexture = this.texture;

			if (newTexture == null)
			{
				if (this.emitterConfig != null && this.emitterConfig.texture != null) {
					this.material.mainTexture = this.emitterConfig.texture;
				}
			}
			else
			{
				this.material.mainTexture = 
				this.currentTexture = this.texture;
			}

			if (this.material.mainTexture != null) {
				this.setMaterialProperties();
			}
		}
		
		private void loadEmitterFile()
		{
			//========================================================
			// PARSE PEX & SETUP EMITTER
			//========================================================
			this.parseBinaryFile();
			this.currentEmitterFile = this.EmitterFile;
			
			this.setupData();
			this.loadTexture();
		}
		
		private void setMaterialProperties()
		{
			if (this.emitterConfig == null)
			{
				return;
			}

			int blendSource = this.blendSource == 0 ? this.emitterConfig.blendFuncSource : this.blendSource -1;
			int blendDestination = this.blendDestination == 0 ? this.emitterConfig.blendFuncDestination : this.blendDestination -1;

			if (this.currentBlendSource != blendSource ||
			    this.currentBlendDestination != blendDestination) {

				this.material.SetInt("_BlendSrcMode", blendSource);
				this.material.SetInt("_BlendDstMode", blendDestination);
				this.material.SetInt("_OpacityModifyRGB", this.emitterConfig.opacityModifyRGB ? 1 : 0);
				
				this.currentBlendSource = blendSource;
				this.currentBlendDestination = blendDestination;
			}
		}
		
		private void OnEnable()
		{
			this.Initialize();
			this.loadEmitterFile();

			#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
				EditorApplication.update += new EditorApplication.CallbackFunction(this.EditorUpdate);
			#endif
		}
		
		private void OnDisable()
		{
			#if UNITY_EDITOR
				EditorApplication.update -= new EditorApplication.CallbackFunction(this.EditorUpdate);
			#endif  
			
			this.ClearDown();
		}
		
		void Reset()
		{
			if (this.mesh != null) {
				this.mesh.Clear();
			}
		}

		void LateUpdate()
		{
			if (!Application.isEditor) {
				UpdateWithDelta(Time.fixedDeltaTime);
			}
		}
		
		void UpdateWithDelta(float aDelta)
		{
			if (this.emitterConfig == null || this.material == null)
			{
				return;
			}

			setMaterialProperties();
						
			// If the emitter is active and the emission rate is greater than zero then emit particles
			if (this.Running && this.emitterConfig.emissionRate > 0.0)
			{
				
				float rate = 1.0f/this.emitterConfig.emissionRate;
				
				if (this.emitterConfig.particleCount < this.emitterConfig.maxParticles)
					this.emitterConfig.emitCounter += aDelta;
				
				while (this.emitterConfig.particleCount < this.emitterConfig.maxParticles && this.emitterConfig.emitCounter > rate)
				{
					this.addParticle();
					this.emitterConfig.emitCounter -= rate;
				}
				
				this.emitterConfig.elapsedTime += aDelta;
				
				if (this.emitterConfig.duration != -1 && this.emitterConfig.duration < this.emitterConfig.elapsedTime)
				{
					this.Running = false;
					return;
				}
			}
			
			// Reset the particle index before updating the particles in this emitter
			int particleIndex = 0;
			
			Vector3[] vertices = this.mesh.vertices;
			Color32[] colors32 = this.mesh.colors32;

			
			// Loop through all the particles updating their location and color
			while (particleIndex < this.emitterConfig.particleCount)
			{
				// Get the particle for the current particle index
				Particle currentParticle = particles[particleIndex];
				
				// Reduce the life span of the particle
				currentParticle.timeToLive -= aDelta;
				
				// If the current particle is alive then update it
				if (currentParticle.timeToLive > 0) {
					
					// If maxRadius is greater than 0 then the particles are going to spin otherwise they are effected by speed and gravity
					if (emitterConfig.emitterType == kParticleTypes.kParticleTypeRadial)
					{
						// Update the angle of the particle from the sourcePosition and the radius.  This is only done of the particles are rotating
						currentParticle.angle += currentParticle.degreesPerSecond * aDelta;
						currentParticle.radius += currentParticle.radiusDelta * aDelta;
						
						Vector2 tmp = new Vector2();
						tmp.x = Convert.ToSingle(emitterConfig.sourcePosition.x - Mathf.Cos(currentParticle.angle) * currentParticle.radius);
						tmp.y = Convert.ToSingle(emitterConfig.sourcePosition.y - Mathf.Sin(currentParticle.angle) * currentParticle.radius);
						currentParticle.position = tmp;

					} else {
						Vector2 tmp, radial, tangential;
						
						radial = new Vector2();
						
						// By default this emitters particles are moved relative to the emitter node position
						Vector2 positionDifference = currentParticle.startPos - new Vector2(0, 0);
						currentParticle.position = currentParticle.position - positionDifference;
						
						if (currentParticle.position.x != 0.0 || currentParticle.position.y != 0.0) {
							radial = currentParticle.position.normalized;
						}

						tangential = radial;
						
						radial.x = radial.x * currentParticle.radialAcceleration;
						radial.y = radial.y * currentParticle.radialAcceleration;
						
						float newy = tangential.x;
						tangential.x = -tangential.y;
						tangential.y = newy;
						tangential.x = tangential.x * currentParticle.tangentialAcceleration;
						tangential.y = tangential.y * currentParticle.tangentialAcceleration;
						
						tmp.x = ((radial.x + tangential.x) + emitterConfig.gravity.x) * aDelta;
						tmp.y = ((radial.y + tangential.y) + emitterConfig.gravity.y) * aDelta;
						
						currentParticle.direction.x += tmp.x;
						currentParticle.direction.y += tmp.y;
						
						tmp.x = currentParticle.direction.x * aDelta;
						tmp.y = currentParticle.direction.y * aDelta;
						
						currentParticle.position.x += tmp.x;
						currentParticle.position.y += tmp.y;
						
						// Now apply the difference calculated early causing the particles to be relative in position to the emitter position
						currentParticle.position += positionDifference;
					}
					
					// Update the particles color
					currentParticle.color.r += (currentParticle.deltaColor.r * aDelta);
					currentParticle.color.g += (currentParticle.deltaColor.g * aDelta);
					currentParticle.color.b += (currentParticle.deltaColor.b * aDelta);
					currentParticle.color.a += (currentParticle.deltaColor.a * aDelta);
					
					// Update the particle size
					currentParticle.particleSize += currentParticle.particleSizeDelta * aDelta;
					currentParticle.particleSize = Math.Max(0, currentParticle.particleSize);
					
					// Update the rotation of the particle
					currentParticle.rotation += currentParticle.rotationDelta * aDelta;
					
					// As we are rendering the particles as quads, we need to define 6 vertices for each particle
					float halfSize = currentParticle.particleSize * 0.5f;
					
					// If a rotation has been defined for this particle then apply the rotation to the vertices that define the particle
					if (currentParticle.rotation != 0.0) {
						float x1 = -halfSize;
						float y1 = -halfSize;
						float x2 = halfSize;
						float y2 = halfSize;
						float x = currentParticle.position.x;
						float y = currentParticle.position.y;
						float r = currentParticle.rotation * Mathf.Deg2Rad;
						float cr = Convert.ToSingle(Mathf.Cos(r));
						float sr = Convert.ToSingle(Mathf.Sin(r));
						float ax = x1 * cr - y1 * sr + x;
						float ay = x1 * sr + y1 * cr + y;
						float bx = x2 * cr - y1 * sr + x;
						float by = x2 * sr + y1 * cr + y;
						float cx = x2 * cr - y2 * sr + x;
						float cy = x2 * sr + y2 * cr + y;
						float dx = x1 * cr - y2 * sr + x;
						float dy = x1 * sr + y2 * cr + y;
						
						vertices[particleIndex * 4 + 0] = new Vector2(ax, ay);
						vertices[particleIndex * 4 + 1] = new Vector2(bx, by);
						vertices[particleIndex * 4 + 2] = new Vector2(cx, cy);
						vertices[particleIndex * 4 + 3] = new Vector2(dx, dy);

					} else {
						// Using the position of the particle, work out the four vertices for the quad that will hold the particle and load those into the quads array.
						float x = currentParticle.position.x;
						float y = currentParticle.position.y;
						
						vertices[particleIndex * 4 + 0] = new Vector2(x - halfSize, y - halfSize);
						vertices[particleIndex * 4 + 1] = new Vector2(x - halfSize, y + halfSize);
						vertices[particleIndex * 4 + 2] = new Vector2(x + halfSize, y + halfSize);
						vertices[particleIndex * 4 + 3] = new Vector2(x + halfSize, y - halfSize);
					}
					
					vertices[particleIndex * 4 + 0].x *= scale;
					vertices[particleIndex * 4 + 0].y *= scale;
					vertices[particleIndex * 4 + 1].x *= scale;
					vertices[particleIndex * 4 + 1].y *= scale;
					vertices[particleIndex * 4 + 2].x *= scale;
					vertices[particleIndex * 4 + 2].y *= scale;
					vertices[particleIndex * 4 + 3].x *= scale;
					vertices[particleIndex * 4 + 3].y *= scale;
						
					colors32[particleIndex * 4 + 0] = 
					colors32[particleIndex * 4 + 1] = 
					colors32[particleIndex * 4 + 2] = 
					colors32[particleIndex * 4 + 3] = new Color32((byte)(255 * currentParticle.color.r), (byte)(255 * currentParticle.color.g), (byte)(255 * currentParticle.color.b), (byte)(255 * currentParticle.color.a));
					
					particles[particleIndex] = currentParticle;
					particleIndex++;

				} else {
					
					// As the particle is not alive anymore replace it with the last active particle 
					// in the array and reduce the count of particles by one.  This causes all active particles
					// to be packed together at the start of the array so that a particle which has run out of
					// life will only drop into this clause once
					if (particleIndex != emitterConfig.particleCount - 1) {
						particles[particleIndex] = particles[emitterConfig.particleCount - 1];
					}
					
					int i = emitterConfig.particleCount - 1;
					vertices[i * 4 + 0] =
					vertices[i * 4 + 1] =
					vertices[i * 4 + 2] =
					vertices[i * 4 + 3] = new Vector2(0, 0);
					
					colors32[i * 4 + 0] =
					colors32[i * 4 + 1] =
					colors32[i * 4 + 2] =
					colors32[i * 4 + 3] = new Color32(0,0,0,0);
					
					emitterConfig.particleCount--;
				}
			}

			if (emitterConfig.particleCount == 0) {
				if (AutoLoop) {
					this.reset();
				}
			}

			mesh.vertices = vertices;
			mesh.colors32 = colors32;
			mesh.RecalculateBounds();
		}
		
		public bool addParticle()
		{
			if (emitterOrigin != null) {
				emitterConfig.sourcePosition = (emitterOrigin.transform.position - gameObject.transform.position) / scale;
			} else {
				emitterConfig.sourcePosition = new Vector2();
			}
						
			// If we have already reached the maximum number of particles then do nothing
			if (emitterConfig.particleCount >= emitterConfig.maxParticles) {
				return false;
			}
			
			Particle particle = particles[emitterConfig.particleCount];
			
			// Init the position of the particle.  This is based on the source position of the particle emitter
			// plus a configured variance.  The RANDOM_MINUS_1_TO_1 macro allows the number to be both positive
			// and negative
			particle.position.x = emitterConfig.sourcePosition.x + emitterConfig.sourcePositionVariance.x * Random.Range(-1.0f, 1.0f);
			particle.position.y = emitterConfig.sourcePosition.y + emitterConfig.sourcePositionVariance.y * Random.Range(-1.0f, 1.0f);
			particle.startPos.x = emitterConfig.sourcePosition.x;
			particle.startPos.y = emitterConfig.sourcePosition.y;
			
			// Init the direction of the particle.  The newAngle is calculated using the angle passed in and the
			// angle variance.
			float newAngle = emitterConfig.angle + emitterConfig.angleVariance * Random.Range(-1.0f, 1.0f);
			
			// Calculate the vectorSpeed using the speed and speedVariance which has been passed in
			float vectorSpeed = emitterConfig.speed + emitterConfig.speedVariance * Random.Range(-1.0f, 1.0f);
			
			// The particles direction vector is calculated by taking the vector calculated above and
			// multiplying that by the speed
			particle.direction.x = vectorSpeed * Mathf.Cos(newAngle);
			particle.direction.y = vectorSpeed * Mathf.Sin(newAngle);

			// Calculate the particles life span using the life span and variance passed in
			particle.timeToLive = Math.Max(0, emitterConfig.lifeSpan + emitterConfig.lifeSpanVariance * Random.Range(-1.0f, 1.0f));
			
			float startRadius = emitterConfig.maxRadius + emitterConfig.maxRadiusVariance * Random.Range(-1.0f, 1.0f);
			float endRadius = emitterConfig.minRadius + emitterConfig.minRadiusVariance * Random.Range(-1.0f, 1.0f);
			
			// Set the default diameter of the particle from the source position
			particle.radius = startRadius;
			particle.radiusDelta = (endRadius - startRadius) / particle.timeToLive;
			particle.angle = (emitterConfig.angle + emitterConfig.angleVariance * Random.Range(-1.0f, 1.0f));
			particle.degreesPerSecond = (emitterConfig.rotatePerSecond + emitterConfig.rotatePerSecondVariance * Random.Range(-1.0f, 1.0f)) * Mathf.Deg2Rad;
			
			particle.radialAcceleration = emitterConfig.radialAcceleration + emitterConfig.radialAccelerationVariance * Random.Range(-1.0f, 1.0f);
			particle.tangentialAcceleration = emitterConfig.tangentialAcceleration + emitterConfig.tangentialAccelerationVariance * Random.Range(-1.0f, 1.0f);
			
			// Calculate the particle size using the start and finish particle sizes
			float particleStartSize = emitterConfig.startParticleSize + emitterConfig.startParticleSizeVariance * Random.Range(-1.0f, 1.0f);
			float particleFinishSize = emitterConfig.finishParticleSize + emitterConfig.finishParticleSizeVariance * Random.Range(-1.0f, 1.0f);
			particle.particleSizeDelta = ((particleFinishSize - particleStartSize) / particle.timeToLive);
			particle.particleSize = Math.Max(0, particleStartSize);
			
			// Calculate the color the particle should have when it starts its life.  All the elements
			// of the start color passed in along with the variance are used to calculate the star color
			Color start = new Color();
			start.r = Mathf.Clamp(emitterConfig.startColor.r + emitterConfig.startColorVariance.r * Random.Range(-1.0f, 1.0f), 0f, 1f);
			start.g = Mathf.Clamp(emitterConfig.startColor.g + emitterConfig.startColorVariance.g * Random.Range(-1.0f, 1.0f), 0f, 1f);
			start.b = Mathf.Clamp(emitterConfig.startColor.b + emitterConfig.startColorVariance.b * Random.Range(-1.0f, 1.0f), 0f, 1f);
			start.a = Mathf.Clamp(emitterConfig.startColor.a + emitterConfig.startColorVariance.a * Random.Range(-1.0f, 1.0f), 0f, 1f);
			
			// Calculate the color the particle should be when its life is over.  This is done the same
			// way as the start color above
			Color end = new Color();
			end.r = Mathf.Clamp(emitterConfig.finishColor.r + emitterConfig.finishColorVariance.r * Random.Range(-1.0f, 1.0f), 0f, 1f);
			end.g = Mathf.Clamp(emitterConfig.finishColor.g + emitterConfig.finishColorVariance.g * Random.Range(-1.0f, 1.0f), 0f, 1f);
			end.b = Mathf.Clamp(emitterConfig.finishColor.b + emitterConfig.finishColorVariance.b * Random.Range(-1.0f, 1.0f), 0f, 1f);
			end.a = Mathf.Clamp(emitterConfig.finishColor.a + emitterConfig.finishColorVariance.a * Random.Range(-1.0f, 1.0f), 0f, 1f);
			
			// Calculate the delta which is to be applied to the particles color during each cycle of its
			// life.  The delta calculation uses the life span of the particle to make sure that the 
			// particles color will transition from the start to end color during its life time.  As the game
			// loop is using a fixed delta value we can calculate the delta color once saving cycles in the 
			// update method
			
			particle.color = start;
			particle.deltaColor.r = ((end.r - start.r) / particle.timeToLive);
			particle.deltaColor.g = ((end.g - start.g) / particle.timeToLive);
			particle.deltaColor.b = ((end.b - start.b) / particle.timeToLive);
			particle.deltaColor.a = ((end.a - start.a) / particle.timeToLive);			
			
			// Calculate the rotation
			float startA = emitterConfig.rotationStart + emitterConfig.rotationStartVariance * Random.Range(-1.0f, 1.0f);
			float endA = emitterConfig.rotationEnd + emitterConfig.rotationEndVariance * Random.Range(-1.0f, 1.0f);
			particle.rotation = startA;
			particle.rotationDelta = (endA - startA) / particle.timeToLive;
			
			particles[emitterConfig.particleCount] = particle;

			// Increment the particle count
			emitterConfig.particleCount++;
						
			// Return YES to show that a particle has been created
			return true;
		}
		
		public void reset()
		{
			Running = true;
			emitterConfig.elapsedTime = 0;
			for (int i = 0; i < emitterConfig.particleCount; i++) {
				// Get the particle for the current particle index
				Particle currentParticle = particles[i];
				currentParticle.timeToLive = 0;
			}
			emitterConfig.emitCounter = 0;
		}
		
		public void setupData()
		{
			if (emitterConfig == null) {
				return;
			}
			
			Vector3[] vertices = new Vector3[emitterConfig.maxParticles * 4];
			Vector2[] uv = new Vector2[emitterConfig.maxParticles * 4];
			int[] triangles = new int[emitterConfig.maxParticles * 6];
			Color32[] colors32 = new Color32[emitterConfig.maxParticles * 4];
			
			for (int i = 0; i < emitterConfig.maxParticles; i++)
			{
				vertices[i * 4 + 0] =
				vertices[i * 4 + 1] =
				vertices[i * 4 + 2] =
				vertices[i * 4 + 3] = new Vector2(0, 0);
				
				uv[i * 4 + 0] = new Vector2(0, 0);
				uv[i * 4 + 1] = new Vector2(0, 1);
				uv[i * 4 + 2] = new Vector2(1, 1);
				uv[i * 4 + 3] = new Vector2(1, 0);
				
				colors32[i * 4 + 0] = new Color32(255, 255, 255, 255);
				colors32[i * 4 + 1] = new Color32(255, 255, 255, 255);
				colors32[i * 4 + 2] = new Color32(255, 255, 255, 255);
				colors32[i * 4 + 3] = new Color32(255, 255, 255, 255);
			}
			
			for (int i = 0; i < emitterConfig.maxParticles; i++)
			{
				triangles[i * 6 + 0] = i * 4 + 0;
				triangles[i * 6 + 1] = i * 4 + 1;
				triangles[i * 6 + 2] = i * 4 + 2;
				
				triangles[i * 6 + 3] = i * 4 + 0;
				triangles[i * 6 + 4] = i * 4 + 2;
				triangles[i * 6 + 5] = i * 4 + 3;
			}

			mesh.Clear();
			mesh.vertices = vertices;
			mesh.colors32 = colors32;
			mesh.uv = uv;
			mesh.triangles = triangles;
		}
		
		#region Editor Update Hooks / Hacks
		
		public void EditorUpdate()
		{
			if (this.EmitterFile != this.currentEmitterFile)
			{
				this.ClearDown();
				this.currentEmitterFile = null;
				
				if (this.EmitterFile != null)
				{
					this.loadEmitterFile();
				}
			}
			
			if (this.texture != this.currentTexture)
			{
				this.material.mainTexture = this.currentTexture = null;
				this.loadTexture();
			}
			
			if (this.RunInEditor)
			{
				this.UpdateWithDelta(Time.fixedDeltaTime);
			}
		}
		
		void ClearDown()
		{
			this.emitterConfig = null;
			this.initialized = false;
		}
		
		#endregion
		
		#region Parsing
		
		private void parseBinaryFile()
		{
			if (this.EmitterFile == null) {
				return;
			}
			
			emitterConfig = new PDUnityParticleEmitter();

			MemoryStream reader = new MemoryStream (this.EmitterFile.bytes);
			
			// Version
			ulong version = ReadULong(reader);
			if (version == 1) {

				// Texture
				emitterConfig.textureName = ReadString(reader);

				long embeddedTexture = ReadLong(reader);
				if (embeddedTexture == 1) {
					emitterConfig.textureData = ReadData(reader);
				}

				// Blending
				emitterConfig.opacityModifyRGB = Convert.ToBoolean(ReadLong(reader));
				emitterConfig.blendFuncSource = UnityBlendModeForGLBlendMode(ReadLong(reader));
				emitterConfig.blendFuncDestination = UnityBlendModeForGLBlendMode(ReadLong(reader));

				// Position Type
				emitterConfig.positionType = ReadLong(reader);

				// Coordinates flipped
				emitterConfig.yScale = ReadLong(reader);

				// Source Position
				emitterConfig.sourcePosition = ReadVector2(reader);
				emitterConfig.sourcePositionVariance = ReadVector2(reader);
				emitterConfig.sourcePosition = new Vector2();

				// Speed
				emitterConfig.speed = ReadFloat(reader);
				emitterConfig.speedVariance = ReadFloat(reader);

				// Life span
				emitterConfig.lifeSpan = ReadFloat(reader);
				emitterConfig.lifeSpanVariance = ReadFloat(reader);
				
				// Angle
				emitterConfig.angle = Mathf.Deg2Rad * ReadFloat(reader);
				emitterConfig.angleVariance = Mathf.Deg2Rad * ReadFloat(reader);

				// Gravity
				emitterConfig.gravity = ReadVector2(reader);

				// Radial and Tangential acceleration
				emitterConfig.radialAcceleration = ReadFloat(reader);
				emitterConfig.tangentialAcceleration = ReadFloat(reader);
				emitterConfig.radialAccelerationVariance = ReadFloat(reader);
				emitterConfig.tangentialAccelerationVariance = ReadFloat(reader);

				// Start color
				emitterConfig.startColor = ReadColor(reader);

				// Start color variance
				emitterConfig.startColorVariance = ReadColor(reader);

				// Finish color
				emitterConfig.finishColor = ReadColor(reader);

				// Start color variance
				emitterConfig.finishColorVariance = ReadColor(reader);
				
				// Max particles
				emitterConfig.maxParticles = ReadLong(reader);

				// particle size
				emitterConfig.startParticleSize = ReadFloat(reader);
				emitterConfig.startParticleSizeVariance = ReadFloat(reader);
				emitterConfig.finishParticleSize = ReadFloat(reader);
				emitterConfig.finishParticleSizeVariance = ReadFloat(reader);

				// Duration
				emitterConfig.duration = ReadFloat(reader);

				// Emitter type
				emitterConfig.emitterType = (kParticleTypes)ReadLong(reader);
				
				// Radius
				emitterConfig.maxRadius = ReadFloat(reader);
				emitterConfig.maxRadiusVariance = ReadFloat(reader);
				emitterConfig.minRadius = ReadFloat(reader);
				emitterConfig.minRadiusVariance = ReadFloat(reader);

				// Rotation
				emitterConfig.rotatePerSecond = ReadFloat(reader);
				emitterConfig.rotatePerSecondVariance = ReadFloat(reader);
				emitterConfig.rotationStart = ReadFloat(reader);
				emitterConfig.rotationStartVariance = ReadFloat(reader);
				emitterConfig.rotationEnd = ReadFloat(reader);
				emitterConfig.rotationEndVariance = ReadFloat(reader);

				// Texture
				emitterConfig.texture = new Texture2D(2, 2, TextureFormat.DXT5, false);
				emitterConfig.texture.LoadImage(emitterConfig.textureData);
				material.mainTexture = emitterConfig.texture;
				
				// Calculate the emission rate
				emitterConfig.emissionRate = emitterConfig.maxParticles / emitterConfig.lifeSpan;
				emitterConfig.emitCounter = 0;
			}
		}
		
		static private string ReadString(MemoryStream fs)
		{
			ulong stringLength = ReadULong(fs);
			
			byte[] buf = new byte[stringLength+1];
			fs.Read(buf, 0, (int)stringLength);
			return System.Text.Encoding.UTF8.GetString(buf);
		}

		static private byte[] ReadData(MemoryStream fs)
		{
			ulong dataLength = ReadULong(fs);

			byte[] buf = new byte[dataLength];
			fs.Read (buf, 0, (int)dataLength);
			return buf;
		}
		
		static private UInt16 ReadChar(MemoryStream fs, int characters)
		{
			string[] s = new string[characters];
			byte[] buf = new byte[Convert.ToByte(s.Length)];
			
			buf = ReadAndSwap(fs, buf.Length);
			return BitConverter.ToUInt16(buf, 0);
		}
		
		static private UInt16 ReadByte(MemoryStream fs)
		{
			byte[] buf = new byte[11];
			buf = ReadAndSwap(fs, buf.Length);
			return BitConverter.ToUInt16(buf, 0);
		}
		
		static private UInt16 ReadUShort(MemoryStream fs)
		{
			byte[] buf = new byte[2];
			buf = ReadAndSwap(fs, buf.Length);
			return BitConverter.ToUInt16(buf, 0);
		}
		
		static private UInt32 ReadULong(MemoryStream fs)
		{
			byte[] buf = new byte[4];
			buf = ReadAndSwap(fs, buf.Length);
			return BitConverter.ToUInt32(buf, 0);
		}

		static private Int16 ReadShort(MemoryStream fs)
		{
			byte[] buf = new byte[2];
			buf = ReadAndSwap(fs, buf.Length);
			return BitConverter.ToInt16(buf, 0);
		}
		
		static private Int32 ReadLong(MemoryStream fs)
		{
			byte[] buf = new byte[4];
			buf = ReadAndSwap(fs, buf.Length);
			return BitConverter.ToInt32(buf, 0);
		}
		
		static private Color ReadColor(MemoryStream fs)
		{
			return new Color(ReadFloat(fs), ReadFloat(fs), ReadFloat(fs), ReadFloat(fs));
		}
		
		static private Vector2 ReadVector2(MemoryStream fs)
		{
			return new Vector2(ReadFloat(fs), ReadFloat(fs));
		}
		
		static private float ReadFloat(MemoryStream fs)
		{
			return (float)ReadDouble(fs);
		}
		
		static private double ReadDouble(MemoryStream fs)
		{
			byte[] buf = new byte[8];
			buf = ReadAndSwap(fs, buf.Length);
			return BitConverter.ToDouble(buf, 0);
		}
		
		static private byte[] ReadAndSwap(MemoryStream fs, int size)
		{
			byte[] buf = new byte[size];
			fs.Read(buf, 0, buf.Length);
			return buf;
		}
		
		#endregion
		
		private int UnityBlendModeForGLBlendMode(int GLBlendMode)
		{
			int blendMode = 0;
			
			switch (GLBlendMode)
			{
			case 0: blendMode = (int)UnityEngine.Rendering.BlendMode.Zero; break;
			case 1: blendMode = (int)UnityEngine.Rendering.BlendMode.One; break;
			case 774: blendMode = (int)UnityEngine.Rendering.BlendMode.DstColor; break;
			case 768: blendMode = (int)UnityEngine.Rendering.BlendMode.SrcColor; break;
			case 775: blendMode = (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor; break;
			case 770: blendMode = (int)UnityEngine.Rendering.BlendMode.SrcAlpha; break;
			case 769: blendMode = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor; break;
			case 772: blendMode = (int)UnityEngine.Rendering.BlendMode.DstAlpha; break;
			case 773: blendMode = (int)UnityEngine.Rendering.BlendMode.OneMinusDstAlpha; break;
			case 776: blendMode = (int)UnityEngine.Rendering.BlendMode.SrcAlphaSaturate; break;
			case 771: blendMode = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha; break;
			}
			
			return blendMode;
		}
	}
}