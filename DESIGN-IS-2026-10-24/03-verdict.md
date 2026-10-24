# Rekomendasi visual dan gameplay — Idle Blacksmith

**Verdict: REDESIGN antarmuka utama, navigasi, dan onboarding agar teks terbaca, aksi dominan jelas, serta produksi–perlengkapan–ekspedisi menjadi satu alur yang dapat diikuti; pertahankan identitas pixel art dan sistem inti.**

Audit heuristik menghasilkan 12/30. Temuan terpenting adalah keterbacaan dan perpindahan konteks, bukan kekurangan jumlah fitur. Bukti berasal dari screenshot kampanye Android yang tersedia dan sumber Unity. Tidak ada playthrough baru atau pengujian perangkat fisik. Detail skor dan batasnya: [scorecard](02-scorecard.md); dasar tiap klaim: [bukti gabungan](01-evidence.md).

## Lima perubahan prioritas

1. **#3/#8 — Keterbacaan:** Bangun ulang skala teks dan ruang kontrol pada lebar logis 360–430: body 16–18, label 14–16, judul 24–30; sediakan pembesaran teks, kotak yang ikut bertambah tinggi, dan target sentuh yang diukur di perangkat. Evidence: [E1/E7](01-evidence.md#e1--typography-and-scale), `WorkshopView.cs:76-82,114,121-122,204-215,229`; `ForgeUI.cs:41-58`.
2. **#3/#5 — Fokus visual:** Jadikan pekerja, alat, dan barang yang sedang diproses sebagai pusat komposisi; tenangkan detail latar, pisahkan label UI dari Nightfall, gunakan cahaya amber pada area aktif, dan tampilkan satu aksi utama. Evidence: [E2](01-evidence.md#e2--visual-focus-and-affordances), `WorkshopView.cs:129-215`; `WorkshopView.Town.cs:23,38`.
3. **#2/#4/#10 — Susunan layar:** Kelompokkan tujuan utama menjadi Bengkel, Hero, dan Petualangan; tempatkan resep/kru di Bengkel, perlengkapan di Hero, serta kartu tujuan aktif yang membawa pemain langsung ke tindakan berikutnya. Evidence: [E3](01-evidence.md#e3--navigation-and-workflow), `WorkshopView.cs:220-265`; `WorkshopView.Campaign.cs:86-203`; `WorkshopView.Modals.cs:13-38,84-136`.
4. **#2/#4/#8 — Alur pertama:** Ganti welcome tunggal dengan panduan buat pedang → pasang ke Rowan → ekspedisi suplai → klaim hasil → pilih peningkatan; kembalikan pemain ke konteks sebelumnya dan jelaskan syarat yang belum terpenuhi di dekat tombolnya. Evidence: [E3/E4/E7](01-evidence.md#e4--first-session-and-item-purpose), `WorkshopView.Modals.cs:40-47`; `WorkshopView.Campaign.cs:129-203`; `WorkshopView.Battle.cs:45-69`.
5. **#2/#6 — Keputusan barang:** Buat pilihan pasang, simpan untuk pesanan, dan jual terbaca setelah tempa; sediakan perlindungan barang, perbandingan statistik, kontrol merchant di konteks penjualan, dan istilah reward/material yang sesuai perilaku. Evidence: [E4/E6](01-evidence.md#e6--honest-labels-and-state-accuracy), `WorkshopSimulation.cs:66-70,212-217,261-264`; `WorkshopView.Modals.cs:53-55,79,123-131`; `WorkshopApp.cs:48`.

## Yang membuat visual sekarang kurang jelas

**Teks kecil berasal dari skala, bukan semata pilihan font.** Root memakai lebar acuan 1080. Label navigasi 21, detail objective 23, dan nilai resource 30 setara 7 / 7,67 / 10 bila komposisi dinormalisasi ke lebar 360. Ini kalkulasi proporsi, bukan pengukuran ukuran fisik Android. Mengganti font saja mempertahankan masalah ukuran tersebut.

**Terlalu banyak tingkat informasi tampil hampir setara.** Nama merek, objective, chapter/reputation, waktu dunia, peta, resource, badge stasiun, serta dock aktif semuanya meminta perhatian. Area kerja pemain perlu terbaca lebih cepat daripada dekorasi dan nama chapter.

**Latar dan objek penting sama-sama gelap.** Pertahankan suasana senja, lalu naikkan pemisahan nilai terang/gelap pada pandai besi, besi panas, anvil, dan bahan yang bergerak. Tempatkan cahaya hangat sebagai penunjuk kerja. Label harus berada pada bidang solid yang tenang dan terpisah dari filter malam.

**Ikon belum menjelaskan tujuan.** World memakai simbol roda gigi yang juga muncul pada Settings; Heroes memakai pickaxe. Gunakan peta/kompas untuk Petualangan, portrait/perisai untuk Hero, dan roda gigi khusus Settings; beri label yang terbaca.

## Spesifikasi awal keterbacaan

Angka berikut adalah target rancangan untuk dicoba, bukan standar yang sudah terbukti cocok untuk seluruh perangkat. Kolom 1080 adalah konversi 3× untuk memudahkan migrasi struktur lama; validasi akhir memakai ukuran logis dan kepadatan perangkat, bukan rumus 3× universal.

| Elemen | Target pada rancangan lebar 360 | Padanan awal pada root 1080 |
|---|---:|---:|
| Isi utama / instruksi | 16–18 | 48–54 |
| Label sekunder yang tetap perlu dibaca | 14–16 | 42–48 |
| Teks tombol utama | 16–18 | 48–54 |
| Judul halaman | 24–30 | 72–90 |
| Nilai penting / hasil tempa | 22–28 | 66–84 |
| Jarak antarkelompok | 16–24 | 48–72 |

Tombol harus menyediakan area sentuh setidaknya **48 × 48 dp** setelah konversi ke perangkat; panduan Android juga membolehkan area sentuh lebih besar daripada ikon yang terlihat. Ukuran dp tidak sama dengan angka unit root Unity. Sumber: [Android Developers — kontrol yang besar dan sederhana](https://developer.android.com/guide/topics/ui/accessibility/views/apps-views).

Sediakan Text size Normal / Besar, dan uji pada pembesaran 125% serta 150%. Perubahan harus memperbesar kotak dan membuat konten mengalir/scroll, karena helper saat ini melakukan pemotongan vertikal. Ini kriteria rancangan usulan, belum fitur yang diterapkan.

## Arah art dan komposisi

- **Bengkel fantasi hangat.** Navy untuk dunia/latar tenang, cream untuk teks, amber untuk besi panas dan aksi utama, teal untuk progres/siap. Jangan mengandalkan warna saja: tetap tulis Bekerja, Bahan kurang, Siap, atau Jeda.
- **Tiga lapisan nilai.** Latar lebih tenang; pekerja dan alat mempunyai siluet kuat; barang aktif serta aksi utama paling mudah ditemukan. Uji komposisi dengan gambar diperkecil ke ukuran ponsel, serta dalam grayscale.
- **Fokus pada pekerjaan yang dipilih.** Saat forge dipilih, tonjolkan pandai besi, anvil, dan item yang dibuat. Eksplorasi pembesaran visual area kerja sekitar 25–40% melalui komposisi/crop, lalu cek overlap; hindari sekadar membesarkan semua sprite di posisi lama.
- **UI tenang, art tetap hidup.** Kurangi label dekoratif dan ruang merek di header. Simpan detail lore pada halaman detail. Hindari menambahkan panel kecil baru untuk menyelesaikan masalah panel kecil yang sudah ada.
- **Skala pixel art konsisten.** Import sudah menggunakan point filtering. Tetapkan grid dan aturan pembesaran yang konsisten; cek ketajaman di ukuran perangkat, karena konstanta Pixel 2,8125 dan skala layar dapat menghasilkan pembesaran pecahan. Jangan menyimpulkan screenshot blur hanya dari angka skala.

## Rekomendasi per layar

| Layar | Perubahan yang paling berdampak |
|---|---|
| Bengkel | Header resource ringkas; satu tujuan aktif; area kerja dominan; dock menyebut resep, bahan tersedia/dibutuhkan, durasi, dan satu aksi utama. Upgrade menjadi aksi sekunder dengan manfaat sebelum → sesudah. |
| Resep | Buka dari forge; tunjukkan resep yang relevan sekarang dan bahan yang kurang; lipat resep terkunci menjadi kelompok Berikutnya. SELECT RECIPE dan CRAFT harus dibedakan jelas; start craft hanya dilakukan ketika pemain memilih aksi tempa yang eksplisit. |
| Hero / perlengkapan | Gabungkan pemilihan hero dan item yang cocok; tampilkan target equip, slot, elemen, serta selisih Power/Guard. Setelah equip, tetap di persiapan hero dengan aksi Lanjut ke ekspedisi. |
| Peta / rute | Region aktif dominan, region masa depan lebih tenang. Setelah memilih region, tampilkan kebutuhan persiapan, weakness dan reward rute. Kunci menjelaskan syarat spesifik, misalnya Klaim quest The Heart of the Fire. |
| Pertarungan | Perbesar HP, countdown heavy attack, dan cooldown; tempatkan telegraph dekat musuh. Mekanik BRACE dan cooldown sudah ada. Perjelas dampaknya lewat pose, gerak benturan ringan, dan feedback damage yang terukur; sediakan pengurangan gerak. |
| Hasil / quest | Victory menampilkan hasil dan langkah berikutnya; defeat menawarkan persiapan gear yang sesuai. Bila boss quest siap, tampilkan klaim/lanjut di konteks hasil agar pemain tidak mencari daftar quest lagi. |

## Gameplay yang perlu diperjelas

**Hasil tempa memiliki tiga tujuan.** Tampilkan keputusan berikut setelah pedang pertama: pasang ke Rowan, simpan untuk permintaan kota, atau jual. Merchant otomatis jangan mengambil gear yang ditandai simpan; jelaskan jika pekerjaan jual yang sudah berjalan telah mereservasi barang. Barang yang sedang dipakai hero sudah dikeluarkan dari inventory oleh sistem yang ada.

**Upgrade harus menjawab masalah yang terlihat.** Dock sekarang menunjukkan harga dan durasi saat ini. Tambahkan dampak berikutnya, misalnya Smelt 2,4 s → 2,1 s. Pada rantai otomatis tunjukkan Menunggu ingot / Menunggu ore, sehingga pemain tahu stasiun mana yang membatasi produksi. Uji keseimbangan setelah informasi jelas; jangan langsung memangkas seluruh timer.

**Pilihan elemen perlu terasa sebelum bertarung.** Efek weakness/resistance sudah ada. Sebelum berangkat, tunjukkan ringkasan apakah senjata sesuai elemen musuh, perubahan damage yang benar, serta apa yang masih kurang. Hindari membuat angka peluang menang yang belum punya model prediksi tervalidasi.

**Perbaiki feedback yang tidak sesuai state.** Penjualan biasa jangan mengatakan reputation naik; job smith harus mengikuti kebutuhan resep; kualitas numerik harus cocok dengan nama tier; instruksi ekspedisi berjalan hanya untuk battle aktif. Hero yang sedang bertarung jangan diarahkan ke halaman rekrut saat pilihan hero ditolak.

## Urutan pengerjaan yang disarankan

1. **Keterbacaan dan hierarki:** token teks, ukuran kontrol, ruang vertikal, label/ikon yang benar, pemisahan UI dari filter malam. Validasi pada rancangan lebar 360, 390, dan 430 serta teks besar.
2. **Satu alur utuh:** bengkel → buat pedang → equip Rowan → pilih rute → hasil → peningkatan. Buat navigasi kembali dan tujuan aktif menyambung alur tersebut, lalu gunakan pola yang sama pada layar lain.
3. **Polesan visual dan feedback:** komposisi bengkel, pencahayaan, hasil tempa, dampak skill, dan perubahan kota yang terbaca. Pertahankan ekonomi dulu agar efek perubahan UI bisa dinilai.
4. **Peningkatan keputusan gameplay:** perlindungan item, perbandingan gear, prediksi dampak upgrade, ringkasan rute/material; balancing dilakukan dengan pengamatan bermain.

## Kriteria untuk menilai rancangan baru

- Pada ukuran ponsel, pemain dapat membaca tugas dan tombol tanpa memperbesar screenshot.
- Pemain baru dapat menyebut tindakan berikutnya dari layar yang terbuka; gunakan target uji menemukan aksi utama dalam 3 detik, sebagai target evaluasi baru, bukan hasil audit.
- Pemain dapat membuat pedang, memasangnya, memulai ekspedisi, dan mengambil hasil tanpa diarahkan pembuat.
- Menutup detail kembali ke daftar/persiapan yang membuka detail itu; posisi scroll dan hero/resep terpilih dipertahankan.
- Bahan kurang, quest terkunci, cooldown, dan gear terkunci menjelaskan sebab serta langkah penyelesaian.
- Tidak ada gear yang ditandai simpan terjual oleh merchant otomatis; item yang sedang diproses punya status yang jelas.
- Besar teks 125%/150% tidak memotong informasi atau tombol penting. Gerak ambient dapat dikurangi.

Untuk melanjutkan ke rencana implementasi, [prompt handoff](04-handoff-prompt.md) sudah memuat keputusan, hal yang dipertahankan, prioritas, dan kriteria verifikasi secara mandiri.
