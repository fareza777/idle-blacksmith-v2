```
/make-plan Redesign antarmuka utama, navigasi, dan onboarding Idle Blacksmith / Ember & Oak. Audit heuristik saat ini 12/30; prinsip mendasar yang bernilai 1 adalah #2 berguna, #4 dapat dipahami, dan #6 jujur.

Verdict paragraph (quoted from 03-verdict.md):
> REDESIGN antarmuka utama, navigasi, dan onboarding agar teks terbaca, aksi dominan jelas, serta produksi–perlengkapan–ekspedisi menjadi satu alur yang dapat diikuti; pertahankan identitas pixel art dan sistem inti.

Mengapa redesign: skala teks, susunan tujuan layar, dan hilangnya konteks antar-dialog saling berkaitan; mengubah warna atau ukuran font saja tidak menyelesaikan alur produksi hingga ekspedisi.

Pengguna utama: pemain baru pada ponsel portrait. Tujuan: mengerti tindakan berikutnya, membuat perlengkapan, memasangnya ke hero, memulai ekspedisi, mengambil hasil, lalu meningkatkan bengkel tanpa petunjuk pembuat.

Konteks dan batasan: proyek Unity 6000.3.20f1 di D:/Idle Blacksmith/Game; UI runtime berada di Assets/ForgeEmpire/Runtime/WorkshopView*.cs dan ForgeUI.cs. Sistem ekonomi/kampanye berada di Assets/ForgeEmpire/Core. Prioritas pengguna adalah visual yang jelas, teks besar, serta workflow mudah dipahami. Audit memakai screenshot Android dan sumber, bukan playthrough baru. Semua ukuran usulan harus divalidasi di perangkat; ukuran unit root Unity bukan otomatis dp/sp.

Preserve from current design:
- Identitas pixel art bengkel, hero, equipment, map, dan battlefields; atlas/sprite helpers: WorkshopView.cs:129-160; WorkshopView.Campaign.cs:9-25; WorkshopView.Battle.cs:25-28.
- Palet dasar navy/cream/gold/teal: ForgeUI.cs:9-16. Perjelas penggunaannya dan pemisahan foreground/background.
- Produksi empat stasiun, kualitas tempa 0-3, otomatisasi serta offline simulation: WorkshopSimulation.cs:45-170. Jangan mengubah data save atau keseimbangan awal tanpa alasan dan rencana terpisah.
- Pilihan gear/elemen/hero, skill aktif yang sudah ada, quest dan restorasi kota: WorkshopSimulation.Campaign.cs:58-158,200-221; WorkshopView.Battle.cs:37-69; WorkshopView.Town.cs:58-67.

Discard:
- Banyak teks ukuran 20-33 pada lebar acuan 1080, setara kira-kira 6,67-11 pada normalisasi lebar 360. WorkshopView.cs:79-81,114,121-122,204-215,229. Mengganggu #3 dan #4.
- Semua tujuan navigasi menjadi modal pengganti tanpa riwayat kembali. Dialog selalu menutup modal sebelumnya: WorkshopView.Modals.cs:13-38; jalur equip membuka inventory umum: WorkshopView.Campaign.cs:145. Mengganggu #2 dan #4.
- Welcome satu layar yang mengajarkan MINE > SMELT > FORGE > SELL, lalu sekaligus menyebut hero dan ekspedisi, tanpa langkah kontekstual setelah TutorialStep=1. WorkshopView.Modals.cs:40-47. Mengganggu #4.
- Petunjuk reward yang tidak sesuai perilaku: reputation dari penjualan biasa/komisi, job smith selalu dua ingot, footer battle selalu terus berjalan. WorkshopApp.cs:48; WorkshopView.Modals.cs:55,79; WorkshopView.Battle.cs:46; WorkshopSimulation.cs:212-217,261-264. Mengganggu #6.

Top five moves from the audit (verbatim):
1. **#3/#8 — Keterbacaan:** Bangun ulang skala teks dan ruang kontrol pada lebar logis 360–430: body 16–18, label 14–16, judul 24–30; sediakan pembesaran teks, kotak yang ikut bertambah tinggi, dan target sentuh yang diukur di perangkat. Evidence: [E1/E7](01-evidence.md#e1--typography-and-scale), `WorkshopView.cs:76-82,114,121-122,204-215,229`; `ForgeUI.cs:41-58`.
2. **#3/#5 — Fokus visual:** Jadikan pekerja, alat, dan barang yang sedang diproses sebagai pusat komposisi; tenangkan detail latar, pisahkan label UI dari Nightfall, gunakan cahaya amber pada area aktif, dan tampilkan satu aksi utama. Evidence: [E2](01-evidence.md#e2--visual-focus-and-affordances), `WorkshopView.cs:129-215`; `WorkshopView.Town.cs:23,38`.
3. **#2/#4/#10 — Susunan layar:** Kelompokkan tujuan utama menjadi Bengkel, Hero, dan Petualangan; tempatkan resep/kru di Bengkel, perlengkapan di Hero, serta kartu tujuan aktif yang membawa pemain langsung ke tindakan berikutnya. Evidence: [E3](01-evidence.md#e3--navigation-and-workflow), `WorkshopView.cs:220-265`; `WorkshopView.Campaign.cs:86-203`; `WorkshopView.Modals.cs:13-38,84-136`.
4. **#2/#4/#8 — Alur pertama:** Ganti welcome tunggal dengan panduan buat pedang → pasang ke Rowan → ekspedisi suplai → klaim hasil → pilih peningkatan; kembalikan pemain ke konteks sebelumnya dan jelaskan syarat yang belum terpenuhi di dekat tombolnya. Evidence: [E3/E4/E7](01-evidence.md#e4--first-session-and-item-purpose), `WorkshopView.Modals.cs:40-47`; `WorkshopView.Campaign.cs:129-203`; `WorkshopView.Battle.cs:45-69`.
5. **#2/#6 — Keputusan barang:** Buat pilihan pasang, simpan untuk pesanan, dan jual terbaca setelah tempa; sediakan perlindungan barang, perbandingan statistik, kontrol merchant di konteks penjualan, dan istilah reward/material yang sesuai perilaku. Evidence: [E4/E6](01-evidence.md#e6--honest-labels-and-state-accuracy), `WorkshopSimulation.cs:66-70,212-217,261-264`; `WorkshopView.Modals.cs:53-55,79,123-131`; `WorkshopApp.cs:48`.

Penjelasan rujukan E1-E7 yang diperlukan untuk memahami prioritas: E1 teks/skala; E2 fokus dan ikon; E3 navigasi/modal; E4 sesi pertama dan nasib barang; E6 ketepatan copy; E7 state/fokus. Semua nama file tanpa awalan berada di Runtime kecuali WorkshopSimulation.cs dan GameContent.cs yang berada di Core. Fakta utama sudah diuraikan di prompt ini.

Redesign principles in priority order:
1. Dapat dipahami (#4): tujuan aktif menyebut satu pekerjaan berikutnya dan membuka konteks kerja yang benar.
2. Berguna (#2): alur buat pedang -> equip Rowan -> ekspedisi suplai -> klaim -> upgrade dapat dilakukan dengan perpindahan yang jelas dan navigasi kembali yang mempertahankan konteks.
3. Estetik/tidak mengganggu (#3/#5): area kerja aktif dominan, informasi penting cukup besar, latar dan ambient mendukung keterbacaan.
4. Jujur/menyeluruh (#6/#8): enabled/disabled cocok dengan aturan core; penjelasan lock/reward/quality cocok dengan mutasi state.

Deliverables for the plan:
- Peta informasi baru dengan tiga tujuan utama Bengkel/Hero/Petualangan dan keputusan tempat tracking quest; jangan hanya menyalin lima tab lama dengan skin baru.
- Wireframe portrait berlabel untuk bengkel, resep, persiapan hero, rute, battle/result; perbandingan langkah alur lama dan alur usulan.
- Token type/spacing/color yang terpusat: target awal pada rancangan lebar 360 adalah body16-18, label14-16, judul24-30. Area sentuh dievaluasi terhadap48x48dp di Android setelah konversi device; jangan memperlakukan satu unit Unity sebagai satu dp.
- Checklist empty/loading bila diperlukan/error/success/focus/disabled; state bahan kurang, forge busy, active expedition, item reserved for sale, cooldown, victory, defeat, dan pending boss-quest claim.
- Rencana migrasi yang mempertahankan save, inventory, quality, selected hero/recipe, pekerja, chapter/quest claims; posisi scroll dan jalur Back ditangani eksplisit.
- Perubahan per target file dan langkah verifikasi yang menguji perilaku pengguna; validasi keterbacaan di lebar logis360/390/430, teks125%/150%, serta pengurangan gerak.
- Cutover: alur pedang-ke-ekspedisi dan seluruh state utama terverifikasi, tidak ada teks/tombol penting terpotong, dan tidak ada regresi save/progres; hapus struktur navigasi lama pada pergantian yang terencana.

Di luar lingkup: menambah region/hero/resep baru, monetisasi, multiplayer, mengganti engine, mengulang semua aset pixel art, atau menyeimbangkan ulang seluruh ekonomi sebelum kejelasan UI diuji.

Anti-patterns:
- Memindahkan struktur lama dengan styling baru tanpa mengurangi detour.
- Membesarkan semua sprite di koordinat lama sampai bertumpuk.
- Menambah panel kecil atau efek ambient untuk menyelesaikan masalah hierarki.
- Membuat angka peluang menang tanpa model prediksi yang diuji.
- Menjaga dua desain navigasi permanen, atau membuang aset/sistem yang masuk daftar Preserve.
```

