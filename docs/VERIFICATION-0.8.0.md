# Verifikasi Living Forge 0.8.0

Pemeriksaan dilakukan pada 27 Oktober 2026 dengan Unity 6000.3.20f1. Perubahan melanjutkan versi 0.7 dari `d091d32d80cdfce213c2e16b0bf26bf83aa503aa` pada branch `feat/illustrated-world`. Kode, artwork pilihan, metadata, panduan dan screenshot disertakan dalam repositori; hasil build dan bukti mentah berada di folder lokal `Builds` dan `QA/living-forge-20261027`.

## Hasil akhir

| Pemeriksaan | Windows | Android API 36 emulator |
| --- | ---: | ---: |
| Aset dan round-trip save Unity pada build release | 196 lulus | 196 lulus |
| Living Forge: battle, crafting, atlas, restorasi, outfit dan teks besar | 15.239 lulus | 15.239 lulus |
| Regresi tampilan dan navigasi | 1.211 lulus | 1.211 lulus |
| Progres lama | 793 lulus | 793 lulus |
| Ilustrasi dan arena lama | 2.019 lulus | 2.019 lulus |

Suite simulasi C# 7.3 dengan warnings sebagai errors lulus **332 tes, 0 gagal**. Suite aset, konten, audio, outfit dan titik pegangan lulus **143 pemeriksaan, 0 gagal**. Kedua suite juga lulus dari ekspor berkas Git staging, bersama validator katalog: **108 resep, 48 musuh dan 12 guardian**, dengan identitas lama tetap utuh.

## Cakupan gameplay dan penyimpanan

Tes meliputi formasi dua/tiga lawan, pergantian fokus, sumber dan penerima serangan yang dikunci, healer/protector/ranged, semua counter guardian, interaksi Frost/stagger/lightning, efek defensif, pause, batas waktu dan kesetaraan advance bertahap/batch/reload. Data lawan skalar lama tetap didukung; data terminal palsu atau slot lawan kosong tidak memberi kemenangan gratis.

Trader, waystone dan bonus sementara diuji dengan biaya serta pemulihan sebenarnya, masa berlaku dua ruang dan penolakan pembayaran berulang. Teknik tempa diuji terhadap statistik serta affinity hasil, termasuk rune utama dan affinity teknik yang berbeda. Timing senjata, fitting dan ukiran memakai helper yang sama pada cursor, area emas, penilaian dan probe. Guided mencatat 86% hanya setelah ketukan sengaja. Ward lama memakai keluarga benda kerja robe yang sesuai.

Perjalanan simulasi dari save baru mencapai enam wilayah dalam **340 keputusan, 0 kekalahan dan 1.217,2 detik simulasi**, dengan Rowan mencapai level 10. Inputnya otomatis dan optimal; angka ini tidak mengukur durasi sesi manusia atau retensi. Probe native memakai sumber daya fixture untuk memeriksa transaksi melalui kontrol sebenarnya, lalu memainkan rute tiga ruang. Galeri dan setup wilayah berikutnya merupakan fixture render, bukan klaim progres yang diperoleh pemain.

## Cakupan visual dan interaksi

Artwork baru terdiri dari 24 atlas musuh berisi dua makhluk × enam pose, dua atlas benda kerja, satu atlas efek cat tangan dan satu atlas delapan senjata. PNG pilihan cocok dengan SHA-256 keluaran imagegen asli. Pemeriksaan alpha memastikan seluruh crop terpisah; tool hanya menulis metadata dan tidak mengedit piksel PNG.

Seluruh 24 lukisan tubuh dasar/outfit mempunyai anotasi dua telapak untuk masing-masing 20 pose: **960 titik pada 480 pose**. Normalizer memeriksa hash, crop dan alpha sumber sebelum menulis koordinat runtime. Armor tetap bagian tubuh lengkap. Pegangan, thrust tubuh dan arah kontak memakai titik yang sama; belati merendah untuk menjangkau makhluk rendah. Probe memeriksa kontak horizontal/vertikal dan sambungan tangan pada 288 kombinasi hero, senjata, ukuran dan kedalaman lawan. Enam galeri tambahan memperlihatkan ready/windup/contact/follow-through pada seluruh outfit.

Review source menemukan jangkauan tombak, pemilihan keluarga Ward dan aksi otomatis Kael yang perlu diperbaiki. Review visual menemukan gagang yang terlepas meskipun tes ujung senjata lulus. Semua temuan tersebut diperbaiki. Review independen atas 12 capture Windows final mengonfirmasi sambungan gagang pada contoh yang diperiksa dan tidak menemukan masalah material tambahan. Screenshot Android untuk formasi tiga musuh, kontak tombak serta bench 150% juga diperiksa langsung. Review still image ini memeriksa keadaan yang ditampilkan; tidak mengukur seluruh kontinuitas animasi.

Arena dan komando diuji pada portrait biasa, layar ringkas 1280, teks 150% serta Reduced motion. Bench lengkap dan kontrol shaping/hasil tetap terlihat pada layout ringkas. Atlas diuji melalui tombol zoom/pan/recenter dan handler drag yang benar. Seluruh enam restorasi diuji pada ruined/tier1/tier2/tier3; satu proyek juga dibayar bertahap melalui kontrol sebenarnya dan menolak pembayaran keempat. Properti workshop mengikuti tingkat yang dimiliki save.

## Paket

ZIP Windows berisi **147 entri**, dengan CRC, daftar berkas dan ukuran setiap entri cocok terhadap folder build, termasuk panduan 0.8. Ukuran **198.002.123 bytes**, SHA-256 `9aa8450512b1275be8bdcfa5620ce6f9bb0e4cf685c14034de932c5a47fd63b2`.

APK release IL2CPP memakai **ARM64**, minimum API **26**, target API **36**, version name **0.8.0** dan version code **8**. AAPT memverifikasi identitas paket `com.emberoak.forgeempire`, activity peluncur dan arsitekturnya; tidak ada flag aplikasi debuggable. Ukuran **172.175.299 bytes**, SHA-256 `5af7a09c1588a81bad48ede6f7b1efe831503047cdb0f2fe388e74ac27a0db0f`.

Sesi Android biasa dibuka tanpa argumen QA dan memuat save normal sebelumnya. Emulator menampilkan dialog “System UI isn't responding”; setelah memilih Wait, tombol kembali ke workshop melalui input OS membuka workshop yang sebenarnya. Ketukan OS pada Adventure juga membuka atlas baru dengan tujuan awal yang benar. Tampilan normal dan screenshot QA Android diperiksa terpisah. Dialog sistem emulator tersebut dicatat; pengujian ini tidak mengukur performa HP fisik.

HP fisik yang sebelumnya diizinkan belum terdeteksi pada pemeriksaan perangkat. APK ini belum dipasang pada HP tersebut. Seluruh eksekusi flag QA Android dibatasi ke emulator dengan save terpisah.

## Batas bukti

Hasil otomatis dan screenshot tidak membuktikan kenikmatan bermain, retensi, FPS atau penggunaan RAM pada HP fisik. Angka textureBytes nol dari probe ilustrasi release tidak dipakai sebagai pengukuran memori. Outfit memakai tiga keluarga per hero dan senjata memakai delapan keluarga siluet; ini bukan desain unik untuk setiap resep. Visual tetap game ilustrasi 2D dengan pose, joint ringan dan tween. Panduan kontrol: [Living Forge](PLAYING-LIVING-FORGE.md); urutan atlas dan titik pegangan: [arah artwork](../ArtSource/living-forge-direction.md).
