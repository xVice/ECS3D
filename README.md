# ECS3D.ECSEngine

ECS3D.ECSEngine is a C# library for building 3D game engines using the Entity-Component-System architecture. It provides functionality for managing game entities, components, cameras, rendering, and more.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
- [Examples](#examples)
- [Contributing](#contributing)
- [License](#license)

## Features

- Entity-Component-System architecture implementation
- Camera handling and movement
- Rendering of 3D objects using OpenGL
- Component-based entity management
- Loading and rendering of skyboxes
- Mouse and keyboard input handling

## Installation

To use ECS3D.ECSEngine in your C# project, follow these steps:

1. Clone or download this repository.
2. Add the necessary ECS3D.ECSEngine source files to your project.
3. Ensure you have the required dependencies installed.

## Usage

Here's a basic example of how to use ECS3D.ECSEngine:

```csharp
using ECS3D.ECSEngine;

// Create an instance of GLControl and pass it to the Engine constructor
GLControl glControl = new GLControl();
Engine engine = new Engine(glControl);

// Create a camera
CameraComponent camera = engine.CreateCamera("MainCamera", new Vector3(0, 0, 3));

// Create a 3D object
GameEntity cubeEntity = engine.Create3DObj("path_to_obj_file.obj", new Vector3(0, 0, 0));

// Set the active camera
engine.SetActiveCamera(camera);

// Move the camera
engine.MoveCam(Keys.W);

// Rotate the camera
engine.RotateCam(new MouseEventArgs(MouseButtons.Left, 1, x, y, 0));

// Render the scene
engine.RenderFrame();
