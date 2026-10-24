# Bukti bobot dan friksi — native Unity

## Sumber dan metode

- Sumber: `Game/Assets/ForgeEmpire/Runtime/WorkshopApp.cs:20-50`, `WorkshopView.cs:267-315`, `WorkshopView.Town.cs:14-67`, `WorkshopView.Modals.cs:40-47,138-145`.
- Pengukuran berkas dilakukan pada sesi audit 24 Oktober 2026 menggunakan metadata berkas lokal. Statistik frame adalah hasil QA yang sudah tersedia, bukan benchmark baru.
- Ini aplikasi native. Initial JS bytes dan jumlah permintaan HTTP awal tidak berlaku sebagai metrik browser; tidak dianggap nol. Waktu interaktif belum diukur.

## Nilai terukur dari berkas

- APK `Builds/Android/ForgeEmpire.apk`: **29.066.084 byte**.
- Folder build Windows: **157.804.778 byte**, **234 berkas**, termasuk runtime Unity.
- `Game/Assets/Resources`: PNG 232.061 byte / 17 berkas; WAV 2.193.864 byte / 6 berkas; JSON 48.507 byte / 17 berkas. Total konten di tiga jenis tersebut **2.474.432 byte**, di luar metadata dan hasil kompresi build. Ini bukan ukuran konten yang seluruhnya pasti berada di memori.
- `QA/native-performance.json`: 240 sampel, median 16,6773 ms, p95 17,7692 ms, allocatedBytes 61.054.724, memoryGrowthBytes -24.152, gen0Collections 0. Perangkat, kondisi capture, dan kesetaraan dengan build terkini tidak dijelaskan lengkap; angka ini tidak membuktikan kinerja di perangkat fisik pengguna.

## Friksi gerak dan perhatian (INFERRED dari sumber)

- Target frame rate selalu 60: `WorkshopApp.cs:22`.
- UI mengubah animasi tiap frame dan memperbarui teks/dialog sekitar setiap 0,12 detik: `WorkshopView.cs:267-315`.
- Pada bengkel idle tanpa hujan, sedikitnya **8 kanal gerak/animasi** dijadwalkan: api tungku, merchant, pose idle pandai besi, asap (`WorkshopView.cs:273-304`), tiga penduduk berpindah posisi, dan perubahan night shade (`WorkshopView.Town.cs:33-49`). Sebagian perubahan sangat pelan dan tidak seluruhnya menarik perhatian setiap saat. Marker tempa juga dihitung terus tetapi disembunyikan di luar tempa, jadi tidak dimasukkan ke delapan kanal tampak.
- Saat hujan aktif, 20 posisi garis hujan berubah setiap frame: `WorkshopView.Town.cs:9,24-26,51-54`.
- Pengaturan yang disediakan: sound, music, haptics; belum ada pengaturan besar teks atau gerak: `WorkshopView.Modals.cs:138-145`.

## Notifikasi dan modal awal (INFERRED)

- Profil baru: satu welcome modal setelah UI dibangun (`WorkshopApp.cs:35-38`); badge quest siap klaim tidak ada sebelum objektif terpenuhi (`WorkshopView.cs:372-376`).
- Profil kembali setelah 60 detik: satu modal offline, bila tutorial sudah dilewati (`WorkshopApp.cs:38`). Pemulihan save dapat menambahkan satu toast (`WorkshopApp.cs:39`). Ini kondisi alternatif, bukan tiga modal serentak.
- Welcome memperkenalkan produksi, kualitas, quest, perlengkapan, hero, dan rute sekaligus dalam satu dialog (`WorkshopView.Modals.cs:44-46`). Setelah Begin, TutorialStep langsung menjadi 1 (`:47`).

## Kekosongan pengukuran

- Tidak ada pengukuran baru cold start / time to interactive / draw call / baterai / pemakaian memori Android.
- `QA/campaign-android-result.json` menunjukkan `passed: false`, `goldenCompleted: false`, dengan emulator terputus. Screenshot dapat dipakai sebagai bukti layar yang tertangkap; hasil itu tidak boleh disebut kelulusan keseluruhan playthrough.
- Tidak ada aplikasi game aktif dalam inventaris jendela saat audit. Build tidak dijalankan ulang dan data simpan tidak diubah.

## Fakta yang relevan bagi prinsip audit

- Berguna/dapat dipahami: onboarding merupakan satu dialog, tanpa langkah kontekstual lanjutan yang terlihat dari jalur ini.
- Tidak mengganggu: ada beberapa gerak ambient bersamaan dengan informasi tugas berukuran kecil.
- Hemat sumber daya/perhatian: frame rate tetap dan gerak selalu aktif; bobot native dan data frame historis dilaporkan terpisah dari skor browser.
- Menyeluruh: pengaturan teks dan motion belum ada; error save dan recovered-save feedback memang tersedia (`WorkshopApp.cs:39,150-155`).
