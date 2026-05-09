cmake_minimum_required(VERSION 3.16)
project(arcanum_spell_engine LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

add_library(arcanum_spell_engine
    src/feature_extractor.cpp
    src/embedding.cpp
    src/balancing.cpp
    src/evolution.cpp
    src/database.cpp
    src/spell_engine.cpp
    src/resolver.cpp
    src/audio.cpp
)

target_include_directories(arcanum_spell_engine PUBLIC include)

add_executable(arcanum_basic_example examples/basic_usage.cpp)
target_link_libraries(arcanum_basic_example PRIVATE arcanum_spell_engine)
