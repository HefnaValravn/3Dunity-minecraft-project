# 🎮 3D Unity Minecraft Project

A feature-rich 3D voxel-based world generator built in Unity, inspired by Minecraft. This project demonstrates advanced procedural generation techniques, optimized chunk loading, and realistic rendering effects.

## ✨ Features

### 🌍 **Procedural World Generation**
- **Multi-biome terrain generation** with smooth biome transitions
  - Plains with configurable flatness
  - Rolling hills with varying heights  
  - Mountain ranges with realistic peaks
- **3D cave systems** with surface entrances and underground networks
- **Layered terrain blocks**: Bedrock, Stone, Dirt, and Grass with proper transitions
- **Infinite world** support with chunk-based loading

### 🎥 **Camera & Controls**
- **Dual camera system**: First-person and third-person perspectives
- **Smooth camera switching** with body visibility toggling
- **Mouse look controls** with configurable sensitivity
- **WASD movement** with Space/Ctrl for vertical movement
- **Physics-based player movement** using Rigidbody

### 🚀 **Performance Optimization**
- **Intelligent chunk management** with view distance culling
- **Frustum culling** to only render visible chunks
- **Occlusion culling** for performance optimization
- **Multi-threaded chunk generation** with coroutines
- **Object pooling** for chunk reuse
- **View direction prioritization** for smoother exploration

### 🌊 **Advanced Water System**
- **Realistic water shader** with reflection and refraction
- **Fresnel effects** for angle-dependent water appearance
- **Skybox reflections** for immersive water surfaces
- **Per-chunk water generation** with tessellation control

### 🌀 **Portal System**
- **Procedural portal generation** in designated chunks
- **Obsidian portal frames** with animated portal cores
- **Video texture portals** with spatial audio
- **Particle effects** for enhanced visual appeal
- **Dynamic portal placement** avoiding water and unstable terrain

### 🎨 **Visual Effects**
- **Custom shaders** for obsidian and portal materials
- **Procedural normal mapping** for realistic surface details
- **Multi-material mesh rendering** with optimized draw calls
- **Skybox integration** for environmental lighting

## 🛠️ Technical Implementation

### **Block System**
```csharp
public enum BlockType
{
    Air, Bedrock, Stone, Dirt, Grass, Obsidian, PortalCore
}
```

### **Chunk Structure**
- **Chunk Size**: 32x128x32 blocks
- **Infinite world** generation with coordinate-based positioning
- **Multi-phase generation**: Terrain → Caves → Water → Portals

### **Optimization Features**
- **Distance-based chunk loading** with circular boundaries
- **Cached calculations** for frequently accessed values
- **Queue-based generation** to prevent frame drops
- **Mesh combining** for reduced draw calls

## 🎮 Controls

| Key | Action |
|-----|--------|
| `W/A/S/D` | Move forward/left/backward/right |
| `Space` | Move up |
| `Left Ctrl` | Move down |
| `C` | Switch between first/third person camera |
| `Mouse` | Look around |

## 🚀 Getting Started

### **Prerequisites**
- Unity 2022.3 LTS or newer
- Universal Render Pipeline (URP)

### **Setup**
1. Clone the repository
2. Open the project in Unity
3. Ensure URP is properly configured
4. Load the "Main Scene" scene found in the "scenes" folder
5. Press Play to start exploring!

### **Project Structure**
```
Assets/
├── Scripts/
│   ├── ChunkManager.cs          # Core chunk management system
│   ├── TerrainGenerator.cs      # Procedural terrain generation
│   ├── ChunkMeshGenerator.cs    # Mesh generation and optimization
│   ├── Player/                  # Player movement and camera scripts
│   └── ...
├── Shaders/
│   ├── WaterShader.shader       # Realistic water rendering
│   ├── Obsidian.shader          # Advanced obsidian material
│   └── portalCore.shader        # Animated portal effects
└── Resources/                   # Block textures and materials
```

## 🔧 Customization

### **Terrain Parameters**
- **Noise Scale**: Controls terrain feature size
- **Biome Settings**: Adjust mountain height and plains flatness  
- **Cave Generation**: Modify cave density and size
- **View Distance**: Configure chunk loading range

### **Performance Tuning**
- **Chunks Per Frame**: Control generation speed vs. frame rate
- **Generation Delay**: Add delays to spread CPU load
- **Occlusion Culling**: Toggle for performance vs. accuracy

### **Visual Settings**
- **Water Level**: Adjust global water height
- **Water Tessellation**: Control water mesh detail
- **Shader Properties**: Customize material appearances

## 🎯 Key Achievements

- ✅ **Infinite procedural world** generation
- ✅ **Optimized performance** with 60+ FPS on modern hardware
- ✅ **Realistic water rendering** with advanced shader effects
- ✅ **Complex cave systems** with proper generation algorithms
- ✅ **Multi-biome support** with smooth transitions
- ✅ **Portal system** with video textures and particle effects
- ✅ **Advanced chunk management** with view frustum culling

## 📈 Performance Metrics

- **Chunk Generation**: ~3 chunks per frame without stuttering
- **View Distance**: Configurable up to 10+ chunks (21x21 grid)
- **Memory Management**: Efficient object pooling and cleanup
- **Render Optimization**: Multi-material meshes with frustum culling

## 🔮 Future Enhancements

- [ ] Block placement/destruction system
- [ ] Advanced lighting with shadows
- [ ] Weather and day/night cycles
- [ ] More biome types and structures

## 📝 License

This project is for educational purposes. Feel free to explore, learn, and build upon it!

---

*Built with Unity 2022.3 LTS • C# • Universal Render Pipeline*
