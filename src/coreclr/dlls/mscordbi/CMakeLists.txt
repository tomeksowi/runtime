
# Set the RPATH of mscordbi so that it can find dependencies without needing to set LD_LIBRARY_PATH
# For more information: http://www.cmake.org/Wiki/CMake_RPATH_handling.
if(CORECLR_SET_RPATH)
  if(CLR_CMAKE_HOST_OSX)
    set(CMAKE_BUILD_WITH_INSTALL_NAME_DIR ON)
    set(CMAKE_INSTALL_NAME_DIR "@rpath")
    set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
    set(CMAKE_INSTALL_RPATH "@loader_path")
  else()
    set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
    set(CMAKE_INSTALL_RPATH "\$ORIGIN")
  endif(CLR_CMAKE_HOST_OSX)
endif(CORECLR_SET_RPATH)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(MSCORDBI_SOURCES
    mscordbi.cpp
)

# Add the DAC PAL export mapping file
if(CLR_CMAKE_HOST_LINUX)
    list(APPEND MSCORDBI_SOURCES ${PAL_REDEFINES_FILE})
endif(CLR_CMAKE_HOST_LINUX)

if(CLR_CMAKE_HOST_WIN32)
    add_definitions(-DFX_VER_INTERNALNAME_STR=mscordbi.dll)

    list(APPEND MSCORDBI_SOURCES
        Native.rc
    )

    set(DEF_SOURCES
        mscordbi.src
    )

    convert_to_absolute_path(DEF_SOURCES ${DEF_SOURCES})

    preprocess_file(${DEF_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/mscordbi.def)

    list(APPEND MSCORDBI_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/mscordbi.def)
else(CLR_CMAKE_HOST_WIN32)
    set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/mscordbi_unixexports.src)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/mscordbi.exports)
    generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})

    if(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS OR CLR_CMAKE_HOST_HAIKU)
        # This option is necessary to ensure that the overloaded new/delete operators defined inside
        # of the utilcode will be used instead of the standard library delete operator.
        set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic")
    endif(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS OR CLR_CMAKE_HOST_HAIKU)

    set_exports_linker_option(${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_WIN32)

add_library_clr(mscordbi SHARED ${MSCORDBI_SOURCES})
set_target_properties(mscordbi PROPERTIES DBI_COMPONENT TRUE)
target_precompile_headers(mscordbi PRIVATE $<$<COMPILE_LANGUAGE:CXX>:stdafx.h>)

if(CLR_CMAKE_HOST_UNIX)
    add_custom_target(mscordbi_exports DEPENDS ${EXPORTS_FILE})
    add_dependencies(mscordbi mscordbi_exports)

    set_property(TARGET mscordbi APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET mscordbi APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_UNIX)

set(COREDBI_LIBRARIES
    debug-pal
    cordbdi
    utilcodestaticnohost
    mdcompiler-dbi
    mdruntime-dbi
    mdruntimerw-dbi
    mddatasource_dbi
    corguids
    minipal
)

if(CLR_CMAKE_HOST_WIN32)

    list(APPEND COREDBI_LIBRARIES
        kernel32.lib
        advapi32.lib
        ole32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        version.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
    )

    target_link_libraries(mscordbi PRIVATE ${COREDBI_LIBRARIES})

elseif(CLR_CMAKE_HOST_UNIX)

    list(APPEND COREDBI_LIBRARIES
        # share the PAL in the dac module
        mscordaccore
    )

    if(CLR_CMAKE_HOST_HAIKU)
        list(APPEND COREDBI_LIBRARIES
            network
        )
    endif(CLR_CMAKE_HOST_HAIKU)

    # Before llvm 16, lld was setting `--undefined-version` by default. The default was
    # flipped to `--no-undefined-version` in lld 16, so we will explicitly set it to
    # `--undefined-version` for our use-case.
    include(CheckLinkerFlag OPTIONAL)
    if(COMMAND check_linker_flag)
        check_linker_flag(CXX -Wl,--undefined-version LINKER_SUPPORTS_UNDEFINED_VERSION)
        if (LINKER_SUPPORTS_UNDEFINED_VERSION)
            add_linker_flag(-Wl,--undefined-version)
        endif(LINKER_SUPPORTS_UNDEFINED_VERSION)
    endif(COMMAND check_linker_flag)

    # COREDBI_LIBRARIES is mentioned twice because ld is one pass linker and will not find symbols
    # if they are defined after they are used. Having all libs twice makes sure that ld will actually
    # find all symbols.
    target_link_libraries(mscordbi PRIVATE ${COREDBI_LIBRARIES} ${COREDBI_LIBRARIES})

    add_dependencies(mscordbi mscordaccore)

    if(CLR_CMAKE_HOST_LINUX)
        add_dependencies(mscordbi pal_redefines_file)
    endif(CLR_CMAKE_HOST_LINUX)

endif(CLR_CMAKE_HOST_WIN32)

esrp_sign(mscordbi)

# add the install targets
install_clr(TARGETS mscordbi DESTINATIONS . sharedFramework COMPONENT debug)
