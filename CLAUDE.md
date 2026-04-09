# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
Unity 2022.3 project — a custom gameplay framework. 

## Running Tests
To run tests we use the unity mcp 

## Architecture
The framework is entity-component-system inspired but not DOTS.
Systems cannot have state, and instead operate on entities (Entity) with specific components (Component<T>).

## Key Dependencies
- **Zenject** — DI (embedded in `Assets/Core/Runtime/Core/Zenject/`)
- **UniTask** — async/await (`com.cysharp.unitask`)
- **Odin Inspector** — editor tooling (`Assets/Plugins/Sirenix/`)
- **URP** — rendering pipeline (Unity 17.3.0)
- **xNode** — visual node editor (GitHub package)



