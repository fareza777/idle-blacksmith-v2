# Idle Blacksmith v2 — Ember & Oak

Game bengkel pandai besi untuk Windows dan Android, dengan workshop bergaya hand-drawn, produksi otomatis, penempaan manual, hero dan ekspedisi. Versi proyek saat ini: **0.5.0**.

<img src="docs/images/workshop-0.4.0.png" alt="Workshop Ember & Oak dengan tujuan membuat pedang untuk Rowan" width="320" />

<img src="docs/images/map-0.5.0.png" alt="Atlas lima wilayah dengan nama dan progres perjalanan" width="260" /> <img src="docs/images/battle-0.5.0.png" alt="Rowan menghadapi guardian Starfall di arena ilustrasi baru" width="260" />

## Baru di 0.5.0

- Dua puluh equipment dengan ilustrasi baru untuk seluruh resep, storage, hero, hasil tempa dan workshop.
- Lima enemy dan lima guardian berbeda, masing-masing dengan empat pose hand-drawn.
- Peta ilustrasi dan lima arena wilayah; artwork yang sama juga dipakai pada hero, cerita dan jurnal.
- Nama seluruh wilayah, tujuan aktif, status restorasi dan preview syarat membuka wilayah terkunci.
- Bayangan yang menapak, gerak kontak/settle, telegraph lembut, serta ikon tinta yang halus.
- Penjelasan pembukaan bab, elemen perlengkapan dan reservasi barang yang lebih tepat. Save dan keseimbangan permainan tetap kompatibel.

Arahan dan prompt produksi: [illustrated-world-generation.md](ArtSource/illustrated-world-generation.md). Aset hasil imagegen disertakan di `Game/Assets/Resources/Art/HandDrawn`; generator analisis hanya membaca alpha dan menulis metadata.

## Alur bermain

Tambang bijih → lebur ingot → tempa perlengkapan → pakai pada hero → jelajahi wilayah → buka perkembangan bengkel berikutnya.

- Lima bab dengan total 35 milestone dan arahan tindakan berikutnya.
- Dua puluh resep, tiga hero, lima wilayah, penyelamatan warga dan 30 cerita.
- Pesanan guild, reputasi, dan koleksi kualitas Masterwork.
- Pekerja otomatis dan progres offline hingga delapan jam.
- Penempaan manual dengan timing pukulan, hasil yang langsung bisa dipakai, dan perlindungan barang untuk tujuan/pesanan.
- Ukuran teks 100/125/150% dan Reduced motion.

Lihat [panduan bermain](docs/PLAYING.md) dan [catatan verifikasi 0.5.0](docs/VERIFICATION-0.5.0.md).

## Membuka proyek

1. Clone repositori ini.
2. Pasang **Unity 6000.3.20f1** melalui Unity Hub. Untuk Android, sertakan Android Build Support, SDK/NDK, dan OpenJDK.
3. Tambahkan folder **`Game`** sebagai proyek Unity, lalu buka.
4. Buka scene `Assets/ForgeEmpire/Workshop.unity` dan tekan Play.

Aset yang dipakai game sudah disertakan. Pembuatan ulang ilustrasi atau audio tidak diperlukan untuk membuka proyek. Font Lato dan Spectral beserta lisensi OFL berada di `Game/Assets/Resources/Fonts`.

## Build

Titik masuk build berada di `Game/Assets/ForgeEmpire/Editor/ForgeBuild.cs`:

| Method | Keluaran |
| --- | --- |
| `ForgeEmpire.Editor.ForgeBuild.Desktop` | `Builds/Windows/ForgeEmpire.exe` |
| `ForgeEmpire.Editor.ForgeBuild.Android` | `Builds/Android/ForgeEmpire.apk` |
| `ForgeEmpire.Editor.ForgeBuild.Delivery` | Windows dan Android |

Jalankan method yang dipilih melalui opsi Unity `-executeMethod`. Contoh PowerShell untuk instalasi Unity standar di Windows:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.20f1/Editor/Unity.exe' `
  -batchmode -quit `
  -projectPath "$PWD/Game" `
  -executeMethod ForgeEmpire.Editor.ForgeBuild.Delivery `
  -logFile "$PWD/QA/build.log"
```

Tutup editor yang sedang memakai proyek sebelum menjalankan build batch. Build Android menggunakan ARM64, minimum API 26, target API 36, dan version code 5. Build script saat ini menghasilkan development build dengan alat pengujian yang aktif hanya melalui argumen khusus.

## Tes

Dengan Node.js dan Unity terpasang di lokasi standar Windows di atas:

```powershell
node tools/test-core.mjs local
node --test tests/art.test.mjs tests/illustrated-art.test.mjs
```

Tes logika memakai compiler C# dan runtime .NET yang disertakan Unity; hasil ditulis ke `QA/`. Jika Unity dipasang di lokasi lain, sesuaikan variabel `data` di `tools/test-core.mjs`.

Untuk menguji APK yang sudah di-build pada emulator Android yang aktif:

```powershell
node tools/test-native-android.mjs progression emulator-5562
node tools/test-native-android.mjs regression emulator-5562
node tools/test-native-android.mjs illustrated emulator-5562
```

Ganti serial dengan serial emulator dari `adb devices`. Runner ini menerima serial emulator saja dan memakai profil simpan QA terpisah. ADB dibaca dari `%LOCALAPPDATA%/Android/Sdk/platform-tools/adb.exe`.

## Isi repositori

| Folder | Isi |
| --- | --- |
| `Game/Assets/ForgeEmpire` | Simulasi, tampilan, animasi, penyimpanan, build dan probe native |
| `Game/Assets/Resources` | Ilustrasi, spritesheet, audio dan font |
| `Game/Packages`, `Game/ProjectSettings` | Dependensi dan pengaturan Unity |
| `ArtSource` | Sumber Aseprite, generator dan arahan ilustrasi |
| `tests`, `tools` | Tes logika/aset dan alat bantu |
| `docs`, `DESIGN-IS-2026-10-24` | Panduan, spesifikasi, rencana dan audit desain |

Cache Unity, hasil build, rekaman QA dan data simpan lokal tidak dimasukkan ke Git.
