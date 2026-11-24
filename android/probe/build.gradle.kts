// Phase 0 の probe アプリ(DeskSlate)。端末の実物を調べて画面に出すだけ。
// 依存は CameraX・activity・ZXing の core だけ(ライセンスは THIRD-PARTY-NOTICES.md)。
plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "io.github.takosasidev.deskslate.probe"
    compileSdk = 35

    defaultConfig {
        applicationId = "io.github.takosasidev.deskslate.probe"
        minSdk = 24
        targetSdk = 35
        versionCode = 1
        versionName = "0.0.1"
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            // ponytail: 配布の署名の鍵は Q-6 が決まるまで debug の鍵で署名する
            signingConfig = signingConfigs.getByName("debug")
        }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    kotlinOptions { jvmTarget = "17" }
}

dependencies {
    // 版の決め方: AGP 8.7.0 / compileSdk 35 で使える最新の安定版(1.6 以降と activity 1.11 以降は compileSdk 36・AGP 8.9.1 が要る)
    val camerax = "1.5.3"
    implementation("androidx.camera:camera-core:$camerax")
    implementation("androidx.camera:camera-camera2:$camerax")
    implementation("androidx.camera:camera-lifecycle:$camerax")
    implementation("androidx.camera:camera-view:$camerax")
    implementation("androidx.activity:activity:1.10.1")
    implementation("com.google.zxing:core:3.5.4")
    testImplementation("junit:junit:4.13.2")
}
