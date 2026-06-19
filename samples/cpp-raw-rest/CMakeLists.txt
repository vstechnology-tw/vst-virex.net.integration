cmake_minimum_required(VERSION 3.20)
project(CppRawRestSample LANGUAGES CXX)

add_executable(cpp-raw-rest main.cpp)
target_compile_features(cpp-raw-rest PRIVATE cxx_std_17)
target_link_libraries(cpp-raw-rest PRIVATE winhttp)

