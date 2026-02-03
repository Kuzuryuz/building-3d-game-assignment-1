cmake_minimum_required(VERSION 3.20)
project(Assignment1 LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_executable(Assignment1
  Assignment1.cpp
  Shader.cpp
)

include(FetchContent)

# ---- GLFW ----
FetchContent_Declare(
  glfw
  GIT_REPOSITORY https://github.com/glfw/glfw.git
  GIT_TAG 3.3.9
)
set(GLFW_BUILD_DOCS OFF CACHE BOOL "" FORCE)
set(GLFW_BUILD_TESTS OFF CACHE BOOL "" FORCE)
set(GLFW_BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(glfw)

# ---- GLAD (OpenGL loader) ----
FetchContent_Declare(
  glad
  GIT_REPOSITORY https://github.com/Dav1dde/glad.git
  GIT_TAG v0.1.36
)
FetchContent_MakeAvailable(glad)

target_link_libraries(Assignment1 PRIVATE glfw glad)

# Copy shaders to the same folder as the exe (so relative path works)
add_custom_command(TARGET Assignment1 POST_BUILD
  COMMAND ${CMAKE_COMMAND} -E copy_if_different
    ${CMAKE_CURRENT_SOURCE_DIR}/shaders/Assignment1.vs
    $<TARGET_FILE_DIR:Assignment1>/Assignment1.vs
)

add_custom_command(TARGET Assignment1 POST_BUILD
  COMMAND ${CMAKE_COMMAND} -E copy_if_different
    ${CMAKE_CURRENT_SOURCE_DIR}/shaders/Assignment1.fs
    $<TARGET_FILE_DIR:Assignment1>/Assignment1.fs
)
