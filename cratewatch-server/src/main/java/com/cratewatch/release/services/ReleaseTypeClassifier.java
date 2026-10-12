package com.cratewatch.release.services;

import jakarta.inject.Singleton;

import java.util.List;

@Singleton
public class ReleaseTypeClassifier {

    public String classify(String primaryType, List<String> secondaryTypes) {
        List<String> secondary = secondaryTypes == null ? List.of() : secondaryTypes;

        if (secondary.contains("Compilation")) {
            return "COMPILATION";
        }
        if (secondary.contains("Live")) {
            return "LIVE";
        }
        if (secondary.contains("Soundtrack")) {
            return "SOUNDTRACK";
        }
        if ("Album".equals(primaryType) && secondary.isEmpty()) {
            return "STUDIO_ALBUM";
        }
        if ("EP".equals(primaryType)) {
            return "EP";
        }
        if ("Single".equals(primaryType)) {
            return "SINGLE";
        }
        return "OTHER";
    }
}
