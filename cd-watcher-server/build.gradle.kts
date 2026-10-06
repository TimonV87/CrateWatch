plugins {
    id("io.micronaut.application") version "4.4.4"
    id("io.micronaut.docker") version "4.4.4"
    id("com.gradleup.shadow") version "8.3.5"
    id("io.micronaut.test-resources") version "4.4.4"
}

version = "0.1"
group = "com.cdwatcher"

val micronautVersion by extra("4.7.3")
val flywayPostgresVersion by extra("11.9.1")
val postgresqlVersion by extra("42.7.4")
val assertjVersion by extra("3.26.3")

repositories {
    mavenCentral()
}

dependencies {
    compileOnly("org.projectlombok:lombok")
    annotationProcessor("org.projectlombok:lombok")
    annotationProcessor("io.micronaut:micronaut-http-validation")
    annotationProcessor("io.micronaut.data:micronaut-data-processor")
    annotationProcessor("io.micronaut.serde:micronaut-serde-processor")
    annotationProcessor("io.micronaut.validation:micronaut-validation-processor")
    annotationProcessor("io.micronaut.security:micronaut-security-annotations")

    implementation("io.micronaut:micronaut-http-client")
    implementation("io.micronaut.data:micronaut-data-jdbc")
    implementation("io.micronaut.sql:micronaut-jdbc-hikari")
    implementation("io.micronaut.flyway:micronaut-flyway")
    implementation("org.flywaydb:flyway-database-postgresql:$flywayPostgresVersion")
    implementation("io.micronaut.serde:micronaut-serde-jackson")
    implementation("io.micronaut.validation:micronaut-validation")
    implementation("jakarta.validation:jakarta.validation-api")
    implementation("io.micronaut.security:micronaut-security-jwt")
    implementation("org.mindrot:jbcrypt:0.4")

    runtimeOnly("org.postgresql:postgresql:$postgresqlVersion")
    runtimeOnly("ch.qos.logback:logback-classic")
    runtimeOnly("org.yaml:snakeyaml")

    testImplementation("io.micronaut:micronaut-http-client")
    testImplementation("org.junit.jupiter:junit-jupiter-api")
    testImplementation("org.assertj:assertj-core:$assertjVersion")
    testRuntimeOnly("org.junit.jupiter:junit-jupiter-engine")
    testCompileOnly("org.projectlombok:lombok")
    testAnnotationProcessor("org.projectlombok:lombok")
}

application {
    mainClass = "com.cdwatcher.Application"
}

java {
    sourceCompatibility = JavaVersion.toVersion("21")
    targetCompatibility = JavaVersion.toVersion("21")
}

micronaut {
    version(micronautVersion)
    runtime("netty")
    testRuntime("junit5")
    processing {
        incremental(true)
        annotations("com.cdwatcher.*")
    }
    testResources {
        additionalModules.add("jdbc-postgresql")
    }
}

tasks.named<Test>("test") {
    useJUnitPlatform()
}
