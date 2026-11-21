// ===== carwash2/levels.js =====
// Mỗi màn giới thiệu đúng 1 thứ (màn 1–10), rồi màn tổng hợp và 5 màn khó.
// v9: car.truck (xe tải dài 3, đẩy xe), switches [{r,c,a:[[r,c]],b:[[r,c]]}] — xem logic.js.
// v10: portals [[[r,c,cạnh],[r,c,cạnh]]] (cổng gắn tường, cạnh N/E/S/W), pylons [{r,c,o:"h"|"v",to:[[r,c,cạnh],[r,c,cạnh]]}] (trụ xoay).
//   tutorial: thêm do:["pyl",k] và focus {face:[r,c,cạnh]} / {pylon:k}.
// Xe: axis 'h'/'v', (r,c) ô trên-trái, len 2 (mặc định). mud: 0 sạch · 1 bám bùn · 2 bùn dày.
// Cổng: side t/b/l/r + chỉ số hàng/cột; color là màu xe (đường hầm) hoặc 'water' (vòi phun).
// sprays = bể rửa trong bãi. tutorial: các bước hướng dẫn nền tối (xem class Tutorial trong game.js):
//   { say, focus: [{car}|{gate:'t:1'}|{cell:[r,c]}|{table:[r,c]}], tap: true }  — chạm để tiếp
//   { say, focus, do: ['move', xe, chiều] | ['rot', xe] }                         — chỉ cho làm đúng nước này
const CW_LEVELS = [
  // 1. Vuốt cho xe chạy
  {
    "name": "Chạm cho xe chạy",
    "tag": "Cơ bản",
    "cols": 4,
    "rows": 5,
    "hint": "<b>Chạm đầu xe</b> muốn đi (hoặc vuốt dọc thân). Xe ra ở <b>đường hầm cùng màu</b>.",
    "gates": [
      {
        "side": "t",
        "i": 1,
        "color": "pink"
      },
      {
        "side": "r",
        "i": 1,
        "color": "blue"
      }
    ],
    "cars": [
      {
        "axis": "h",
        "r": 1,
        "c": 0,
        "color": "blue"
      },
      {
        "axis": "v",
        "r": 2,
        "c": 1,
        "color": "pink"
      }
    ],
    "tutorial": [
      {
        "say": "Mỗi xe phải ra đúng đường hầm cùng màu với nó.",
        "focus": [
          {
            "car": 1
          },
          {
            "gate": "t:1"
          }
        ],
        "tap": true
      },
      {
        "say": "Xe xanh đang chắn đường. Vuốt xe xanh sang phải.",
        "focus": [
          {
            "car": 0
          },
          {
            "gate": "r:1"
          }
        ],
        "do": [
          "move",
          0,
          1
        ]
      },
      {
        "say": "Giờ vuốt xe hồng lên.",
        "focus": [
          {
            "car": 1
          },
          {
            "gate": "t:1"
          }
        ],
        "do": [
          "move",
          1,
          -1
        ]
      }
    ]
  },
  // 2. Bể rửa
  {
    "name": "Bể rửa",
    "tag": "Mới",
    "cols": 4,
    "rows": 6,
    "hint": "Xe còn bùn phải lăn qua <b>bể rửa</b> trước khi vào đường hầm.",
    "gates": [
      {
        "side": "t",
        "i": 1,
        "color": "pink"
      },
      {
        "side": "b",
        "i": 3,
        "color": "blue"
      },
      {
        "side": "r",
        "i": 2,
        "color": "green"
      }
    ],
    "sprays": [
      [
        1,
        1
      ],
      [
        4,
        3
      ]
    ],
    "cars": [
      {
        "axis": "v",
        "r": 3,
        "c": 1,
        "color": "pink",
        "mud": 1
      },
      {
        "axis": "v",
        "r": 0,
        "c": 3,
        "color": "blue",
        "mud": 1
      },
      {
        "axis": "h",
        "r": 2,
        "c": 2,
        "color": "green"
      }
    ],
    "tutorial": [
      {
        "say": "Xe này còn bùn. Đường hầm không nhận xe bẩn.",
        "focus": [
          {
            "car": 0
          }
        ],
        "tap": true
      },
      {
        "say": "Đây là bể rửa. Xe lăn qua là sạch bùn.",
        "focus": [
          {
            "cell": [
              1,
              1
            ]
          }
        ],
        "tap": true
      },
      {
        "say": "Chạm/vuốt xe hồng lên: qua bể rửa rồi ra đường hầm hồng.",
        "focus": [
          {
            "car": 0
          },
          {
            "cell": [
              1,
              1
            ]
          },
          {
            "gate": "t:1"
          }
        ],
        "do": [
          "move",
          0,
          -1
        ]
      }
    ]
  },
  // 3. Vòi phun
  {
    "name": "Vòi phun",
    "tag": "Mới",
    "cols": 5,
    "rows": 6,
    "hint": "<b>Vòi phun</b> ở mép bãi xịt sạch bùn rồi <b>đẩy xe lại</b> — không phải lối ra.",
    "gates": [
      {
        "side": "t",
        "i": 1,
        "color": "water"
      },
      {
        "side": "b",
        "i": 1,
        "color": "pink"
      },
      {
        "side": "r",
        "i": 2,
        "color": "blue"
      },
      {
        "side": "l",
        "i": 2,
        "color": "water"
      },
      {
        "side": "b",
        "i": 3,
        "color": "green"
      },
      {
        "side": "l",
        "i": 4,
        "color": "yellow"
      }
    ],
    "cars": [
      {
        "axis": "v",
        "r": 2,
        "c": 1,
        "color": "pink",
        "mud": 1
      },
      {
        "axis": "h",
        "r": 2,
        "c": 3,
        "color": "blue",
        "mud": 1
      },
      {
        "axis": "v",
        "r": 3,
        "c": 3,
        "color": "green"
      },
      {
        "axis": "h",
        "r": 4,
        "c": 1,
        "color": "yellow"
      }
    ],
    "tutorial": [
      {
        "say": "Đây là vòi phun: xịt sạch bùn rồi đẩy xe ngược lại. Sau lưng nó là tường, không có đường ra.",
        "focus": [
          {
            "gate": "t:1"
          }
        ],
        "tap": true
      },
      {
        "say": "Dọn đường trước: vuốt xe vàng sang trái.",
        "focus": [
          {
            "car": 3
          },
          {
            "gate": "l:4"
          }
        ],
        "do": [
          "move",
          3,
          -1
        ]
      },
      {
        "say": "Chạm/vuốt xe hồng lên vòi phun. Sạch bùn, bị đẩy xuống và ra luôn đường hầm hồng.",
        "focus": [
          {
            "car": 0
          },
          {
            "gate": "t:1"
          },
          {
            "gate": "b:1"
          }
        ],
        "do": [
          "move",
          0,
          -1
        ]
      }
    ]
  },
  // 4. Bùn văng
  {
    "name": "Bùn văng",
    "tag": "Mới",
    "cols": 5,
    "rows": 5,
    "splash": true,
    "hint": "Xe bùn đâm vào xe sạch là <b>văng bùn</b> sang xe kia.",
    "gates": [
      {
        "side": "l",
        "i": 1,
        "color": "pink"
      },
      {
        "side": "r",
        "i": 1,
        "color": "water"
      },
      {
        "side": "t",
        "i": 3,
        "color": "blue"
      },
      {
        "side": "b",
        "i": 1,
        "color": "green"
      },
      {
        "side": "r",
        "i": 4,
        "color": "yellow"
      },
      {
        "side": "t",
        "i": 4,
        "color": "water"
      },
      {
        "side": "b",
        "i": 4,
        "color": "blue"
      }
    ],
    "cars": [
      {
        "axis": "h",
        "r": 1,
        "c": 0,
        "color": "pink",
        "mud": 1
      },
      {
        "axis": "v",
        "r": 0,
        "c": 3,
        "color": "blue"
      },
      {
        "axis": "v",
        "r": 2,
        "c": 1,
        "color": "green"
      },
      {
        "axis": "h",
        "r": 4,
        "c": 3,
        "color": "yellow"
      },
      {
        "axis": "v",
        "r": 1,
        "c": 4,
        "color": "blue",
        "mud": 1
      }
    ],
    "tutorial": [
      {
        "say": "Xe còn bùn lao vào xe sạch thì bùn văng sang xe kia!",
        "focus": [
          {
            "car": 4
          },
          {
            "car": 3
          }
        ],
        "tap": true
      },
      {
        "say": "Làn của xe vàng không có vòi phun hay bể rửa: dính bùn là kẹt. Tính thứ tự cho khéo.",
        "focus": [
          {
            "car": 3
          },
          {
            "gate": "r:4"
          }
        ],
        "tap": true
      }
    ]
  },
  // 5. Vũng bùn
  {
    "name": "Vũng bùn",
    "tag": "Mới",
    "cols": 5,
    "rows": 5,
    "splash": true,
    "hint": "Xe đầu tiên qua <b>vũng bùn</b> sẽ dính hết bùn, rồi vũng biến mất.",
    "gates": [
      {
        "side": "t",
        "i": 2,
        "color": "water"
      },
      {
        "side": "b",
        "i": 2,
        "color": "green"
      },
      {
        "side": "l",
        "i": 2,
        "color": "pink"
      },
      {
        "side": "t",
        "i": 0,
        "color": "blue"
      },
      {
        "side": "r",
        "i": 4,
        "color": "yellow"
      }
    ],
    "muds": [
      [
        2,
        2
      ]
    ],
    "cars": [
      {
        "axis": "v",
        "r": 3,
        "c": 2,
        "color": "green"
      },
      {
        "axis": "h",
        "r": 2,
        "c": 3,
        "color": "pink"
      },
      {
        "axis": "v",
        "r": 0,
        "c": 0,
        "color": "blue"
      },
      {
        "axis": "h",
        "r": 4,
        "c": 3,
        "color": "yellow"
      }
    ],
    "tutorial": [
      {
        "say": "Vũng bùn: xe đầu tiên lăn qua sẽ dính hết bùn, rồi vũng biến mất.",
        "focus": [
          {
            "cell": [
              2,
              2
            ]
          }
        ],
        "tap": true
      },
      {
        "say": "Cho xe xanh lá hứng bùn: làn của nó có vòi phun để rửa lại.",
        "focus": [
          {
            "car": 0
          },
          {
            "cell": [
              2,
              2
            ]
          },
          {
            "gate": "t:2"
          }
        ],
        "do": [
          "move",
          0,
          -1
        ]
      }
    ]
  },
  // 6. Vũng sơn
  {
    "name": "Vũng sơn",
    "tag": "Giữ lại",
    "cols": 5,
    "rows": 5,
    "hint": "Lăn qua <b>vũng sơn</b> thì xe đổi màu. Có xe phải qua vũng rồi quay lại.",
    "gates": [
      {
        "side": "r",
        "i": 2,
        "color": "blue"
      },
      {
        "side": "t",
        "i": 1,
        "color": "yellow"
      },
      {
        "side": "r",
        "i": 4,
        "color": "yellow"
      },
      {
        "side": "t",
        "i": 3,
        "color": "blue"
      }
    ],
    "paints": [
      {
        "r": 2,
        "c": 0,
        "color": "blue"
      },
      {
        "r": 1,
        "c": 1,
        "color": "yellow"
      }
    ],
    "cars": [
      {
        "axis": "h",
        "r": 2,
        "c": 1,
        "color": "pink"
      },
      {
        "axis": "v",
        "r": 3,
        "c": 1,
        "color": "green"
      },
      {
        "axis": "h",
        "r": 4,
        "c": 3,
        "color": "yellow"
      },
      {
        "axis": "v",
        "r": 0,
        "c": 3,
        "color": "blue"
      }
    ],
    "tutorial": [
      {
        "say": "Lăn qua vũng sơn thì xe đổi sang màu của vũng.",
        "focus": [
          {
            "car": 0
          },
          {
            "cell": [
              2,
              0
            ]
          }
        ],
        "tap": true
      },
      {
        "say": "Chạm/vuốt xe hồng sang trái để nhúng sơn xanh.",
        "focus": [
          {
            "car": 0
          },
          {
            "cell": [
              2,
              0
            ]
          }
        ],
        "do": [
          "move",
          0,
          -1
        ]
      },
      {
        "say": "Xe đã thành màu xanh. Vuốt sang phải ra đường hầm xanh.",
        "focus": [
          {
            "car": 0
          },
          {
            "gate": "r:2"
          }
        ],
        "do": [
          "move",
          0,
          1
        ]
      }
    ]
  },
  // 7. Bùn dày
  {
    "name": "Bùn dày",
    "tag": "Mới",
    "cols": 5,
    "rows": 6,
    "splash": true,
    "hint": "<b>Bùn dày</b> che mất màu xe. Lăn qua <b>bể rửa</b> để lộ màu.",
    "gates": [
      {
        "side": "t",
        "i": 2,
        "color": "pink"
      },
      {
        "side": "b",
        "i": 2,
        "color": "blue"
      },
      {
        "side": "r",
        "i": 1,
        "color": "yellow"
      },
      {
        "side": "l",
        "i": 4,
        "color": "green"
      },
      {
        "side": "b",
        "i": 4,
        "color": "blue"
      }
    ],
    "sprays": [
      [
        1,
        2
      ],
      [
        4,
        1
      ]
    ],
    "cars": [
      {
        "axis": "v",
        "r": 3,
        "c": 2,
        "color": "pink",
        "mud": 2
      },
      {
        "axis": "h",
        "r": 1,
        "c": 0,
        "color": "yellow",
        "mud": 2
      },
      {
        "axis": "h",
        "r": 4,
        "c": 3,
        "color": "green",
        "mud": 2
      },
      {
        "axis": "v",
        "r": 0,
        "c": 4,
        "color": "blue"
      }
    ],
    "tutorial": [
      {
        "say": "Bùn dày che mất màu xe, chưa biết xe màu gì.",
        "focus": [
          {
            "car": 0
          }
        ],
        "tap": true
      },
      {
        "say": "Chạm/vuốt lên qua bể rửa để lộ màu.",
        "focus": [
          {
            "car": 0
          },
          {
            "cell": [
              1,
              2
            ]
          },
          {
            "gate": "t:2"
          }
        ],
        "do": [
          "move",
          0,
          -1
        ]
      }
    ]
  },
  // 8. Bàn xoay
  {
    "name": "Bàn xoay",
    "tag": "Mới",
    "cols": 5,
    "rows": 5,
    "hint": "Xe đứng gọn trên <b>bàn xoay</b>: vuốt ngang thân xe để xoay 90°.",
    "gates": [
      {
        "side": "t",
        "i": 2,
        "color": "yellow"
      },
      {
        "side": "l",
        "i": 2,
        "color": "blue"
      },
      {
        "side": "r",
        "i": 2,
        "color": "pink"
      },
      {
        "side": "l",
        "i": 0,
        "color": "blue"
      },
      {
        "side": "r",
        "i": 4,
        "color": "green"
      }
    ],
    "tables": [
      [
        2,
        1
      ]
    ],
    "cars": [
      {
        "axis": "h",
        "r": 2,
        "c": 1,
        "color": "yellow"
      },
      {
        "axis": "h",
        "r": 2,
        "c": 3,
        "color": "pink"
      },
      {
        "axis": "h",
        "r": 0,
        "c": 0,
        "color": "blue"
      },
      {
        "axis": "h",
        "r": 4,
        "c": 1,
        "color": "green"
      }
    ],
    "tutorial": [
      {
        "say": "Bàn xoay: xe đứng gọn trên bàn thì xoay được.",
        "focus": [
          {
            "table": [
              2,
              1
            ]
          }
        ],
        "tap": true
      },
      {
        "say": "Vuốt ngang thân xe để xoay 90°.",
        "focus": [
          {
            "car": 0
          }
        ],
        "do": [
          "rot",
          0
        ]
      },
      {
        "say": "Giờ vuốt lên, ra đường hầm vàng.",
        "focus": [
          {
            "car": 0
          },
          {
            "gate": "t:2"
          }
        ],
        "do": [
          "move",
          0,
          -1
        ]
      }
    ]
  },
  // 9. Barie
  {
    "name": "Barie",
    "tag": "Tap-away",
    "cols": 5,
    "rows": 5,
    "hint": "<b>Barie</b> mở khi rửa đủ số xe ghi trên biển.",
    "gates": [
      {
        "side": "t",
        "i": 2,
        "color": "pink"
      },
      {
        "side": "l",
        "i": 4,
        "color": "blue"
      },
      {
        "side": "r",
        "i": 0,
        "color": "yellow"
      },
      {
        "side": "b",
        "i": 0,
        "color": "green"
      }
    ],
    "barriers": [
      {
        "r": 1,
        "c": 2,
        "need": 2
      }
    ],
    "cars": [
      {
        "axis": "v",
        "r": 2,
        "c": 2,
        "color": "pink"
      },
      {
        "axis": "h",
        "r": 4,
        "c": 3,
        "color": "blue"
      },
      {
        "axis": "h",
        "r": 0,
        "c": 0,
        "color": "yellow"
      },
      {
        "axis": "v",
        "r": 2,
        "c": 0,
        "color": "green"
      }
    ],
    "tutorial": [
      {
        "say": "Barie mở khi rửa đủ số xe ghi trên biển.",
        "focus": [
          {
            "cell": [
              1,
              2
            ]
          }
        ],
        "tap": true
      }
    ]
  },
  // 10. Đèn giao thông
  {
    "name": "Đèn giao thông",
    "tag": "Tap-away",
    "cols": 5,
    "rows": 5,
    "hint": "<b>Đèn đỏ</b> chặn đường. Sau mỗi lượt đèn đổi màu.",
    "gates": [
      {
        "side": "t",
        "i": 0,
        "color": "pink"
      },
      {
        "side": "r",
        "i": 3,
        "color": "blue"
      },
      {
        "side": "l",
        "i": 0,
        "color": "yellow"
      },
      {
        "side": "b",
        "i": 4,
        "color": "green"
      }
    ],
    "lights": [
      {
        "r": 1,
        "c": 0,
        "phase": 1
      },
      {
        "r": 3,
        "c": 3,
        "phase": 0
      }
    ],
    "cars": [
      {
        "axis": "v",
        "r": 3,
        "c": 0,
        "color": "pink"
      },
      {
        "axis": "h",
        "r": 3,
        "c": 1,
        "color": "blue"
      },
      {
        "axis": "h",
        "r": 0,
        "c": 2,
        "color": "yellow"
      },
      {
        "axis": "v",
        "r": 0,
        "c": 4,
        "color": "green"
      }
    ],
    "tutorial": [
      {
        "say": "Đèn đổi màu sau mỗi lượt. Đèn đỏ chặn đường.",
        "focus": [
          {
            "cell": [
              1,
              0
            ]
          },
          {
            "cell": [
              3,
              3
            ]
          }
        ],
        "tap": true
      }
    ]
  },
  // 11. Tổng hợp
  {
    "name": "Tổng hợp",
    "tag": "Tổng hợp",
    "cols": 6,
    "rows": 7,
    "splash": true,
    "hint": "Gộp vòi phun, bùn văng, vũng bùn và bể rửa. Mẹo: để xe <b>đã bẩn</b> hút vũng bùn.",
    "gates": [
      {
        "side": "t",
        "i": 1,
        "color": "water"
      },
      {
        "side": "b",
        "i": 1,
        "color": "pink"
      },
      {
        "side": "l",
        "i": 3,
        "color": "water"
      },
      {
        "side": "r",
        "i": 3,
        "color": "blue"
      },
      {
        "side": "t",
        "i": 4,
        "color": "yellow"
      },
      {
        "side": "b",
        "i": 4,
        "color": "green"
      },
      {
        "side": "b",
        "i": 2,
        "color": "orange"
      }
    ],
    "muds": [
      [
        3,
        2
      ]
    ],
    "sprays": [
      [
        4,
        4
      ]
    ],
    "cars": [
      {
        "axis": "v",
        "r": 4,
        "c": 1,
        "color": "pink",
        "mud": 1
      },
      {
        "axis": "h",
        "r": 3,
        "c": 3,
        "color": "blue",
        "mud": 1
      },
      {
        "axis": "v",
        "r": 0,
        "c": 4,
        "color": "yellow"
      },
      {
        "axis": "v",
        "r": 5,
        "c": 4,
        "color": "green",
        "mud": 1
      },
      {
        "axis": "v",
        "r": 0,
        "c": 2,
        "color": "orange"
      }
    ]
  },
  // 12. Thứ tự rửa
  {
    "name": "Thứ tự rửa",
    "tag": "Khó",
    "cols": 6,
    "rows": 6,
    "splash": true,
    "hint": "<b>Màn khó.</b> Xe bùn lao vào xe sạch là hỏng cả hai. Nghĩ xem xe nào phải đi trước.",
    "gates": [
      {
        "side": "t",
        "i": 0,
        "color": "water"
      },
      {
        "side": "b",
        "i": 0,
        "color": "pink"
      },
      {
        "side": "r",
        "i": 1,
        "color": "yellow"
      },
      {
        "side": "b",
        "i": 3,
        "color": "blue"
      },
      {
        "side": "r",
        "i": 4,
        "color": "water"
      },
      {
        "side": "l",
        "i": 4,
        "color": "green"
      },
      {
        "side": "l",
        "i": 5,
        "color": "orange"
      },
      {
        "side": "t",
        "i": 5,
        "color": "purple"
      }
    ],
    "cars": [
      {
        "axis": "v",
        "r": 3,
        "c": 0,
        "color": "pink",
        "mud": 1
      },
      {
        "axis": "h",
        "r": 1,
        "c": 0,
        "color": "yellow"
      },
      {
        "axis": "v",
        "r": 0,
        "c": 3,
        "color": "blue"
      },
      {
        "axis": "h",
        "r": 4,
        "c": 2,
        "color": "green",
        "mud": 1
      },
      {
        "axis": "h",
        "r": 5,
        "c": 3,
        "color": "orange"
      },
      {
        "axis": "v",
        "r": 4,
        "c": 5,
        "color": "purple"
      }
    ]
  },
  // 13. Xe hy sinh
  {
    "name": "Xe hy sinh",
    "tag": "Khó",
    "cols": 5,
    "rows": 6,
    "splash": true,
    "hint": "<b>Màn khó.</b> Phải có xe chịu hứng vũng bùn. Xe nào có vòi phun trong làn?",
    "gates": [
      {
        "side": "t",
        "i": 2,
        "color": "pink"
      },
      {
        "side": "l",
        "i": 2,
        "color": "green"
      },
      {
        "side": "r",
        "i": 2,
        "color": "water"
      },
      {
        "side": "r",
        "i": 0,
        "color": "yellow"
      },
      {
        "side": "b",
        "i": 4,
        "color": "blue"
      }
    ],
    "muds": [
      [
        2,
        2
      ]
    ],
    "cars": [
      {
        "axis": "v",
        "r": 3,
        "c": 2,
        "color": "pink"
      },
      {
        "axis": "h",
        "r": 2,
        "c": 3,
        "color": "green"
      },
      {
        "axis": "h",
        "r": 0,
        "c": 1,
        "color": "yellow"
      },
      {
        "axis": "v",
        "r": 0,
        "c": 4,
        "color": "blue"
      }
    ]
  },
  // 14. Xoay sau khi rửa
  {
    "name": "Xoay sau khi rửa",
    "tag": "Khó",
    "cols": 5,
    "rows": 6,
    "splash": true,
    "hint": "<b>Màn khó.</b> Rửa xong xe bị đẩy về đâu? Phải giữ được nó trên bàn xoay.",
    "gates": [
      {
        "side": "l",
        "i": 2,
        "color": "water"
      },
      {
        "side": "t",
        "i": 2,
        "color": "yellow"
      },
      {
        "side": "b",
        "i": 3,
        "color": "blue"
      },
      {
        "side": "r",
        "i": 4,
        "color": "green"
      },
      {
        "side": "t",
        "i": 4,
        "color": "water"
      },
      {
        "side": "b",
        "i": 4,
        "color": "pink"
      }
    ],
    "tables": [
      [
        2,
        1
      ]
    ],
    "cars": [
      {
        "axis": "h",
        "r": 2,
        "c": 1,
        "color": "yellow",
        "mud": 1
      },
      {
        "axis": "v",
        "r": 2,
        "c": 3,
        "color": "blue"
      },
      {
        "axis": "h",
        "r": 4,
        "c": 3,
        "color": "green"
      },
      {
        "axis": "v",
        "r": 0,
        "c": 4,
        "color": "pink",
        "mud": 1
      }
    ]
  },
  // 15. Đổi màu đúng lúc
  {
    "name": "Đổi màu đúng lúc",
    "tag": "Khó",
    "cols": 5,
    "rows": 6,
    "splash": true,
    "hint": "<b>Màn khó.</b> Xe tím phải qua vũng sơn rồi quay lại, nhưng đứng sai lúc sẽ chắn đường xe khác.",
    "gates": [
      {
        "side": "r",
        "i": 3,
        "color": "pink"
      },
      {
        "side": "t",
        "i": 1,
        "color": "water"
      },
      {
        "side": "b",
        "i": 1,
        "color": "green"
      },
      {
        "side": "r",
        "i": 5,
        "color": "water"
      },
      {
        "side": "l",
        "i": 5,
        "color": "yellow"
      },
      {
        "side": "t",
        "i": 0,
        "color": "pink"
      },
      {
        "side": "b",
        "i": 4,
        "color": "blue"
      }
    ],
    "paints": [
      {
        "r": 3,
        "c": 0,
        "color": "pink"
      }
    ],
    "barriers": [
      {
        "r": 3,
        "c": 4,
        "need": 2
      }
    ],
    "cars": [
      {
        "axis": "h",
        "r": 3,
        "c": 2,
        "color": "purple"
      },
      {
        "axis": "v",
        "r": 0,
        "c": 1,
        "color": "green",
        "mud": 1
      },
      {
        "axis": "h",
        "r": 5,
        "c": 2,
        "color": "yellow",
        "mud": 1
      },
      {
        "axis": "v",
        "r": 4,
        "c": 0,
        "color": "orange"
      },
      {
        "axis": "v",
        "r": 0,
        "c": 4,
        "color": "blue"
      }
    ]
  },
  // 16. Tổng lực
  {
    "name": "Tổng lực",
    "tag": "Khó nhất",
    "cols": 6,
    "rows": 6,
    "splash": true,
    "hint": "<b>Màn khó nhất.</b> Vòi phun, bùn văng và bàn xoay cùng lúc. 9 lượt là giải được.",
    "gates": [
      {
        "side": "t",
        "i": 0,
        "color": "water"
      },
      {
        "side": "b",
        "i": 0,
        "color": "pink"
      },
      {
        "side": "r",
        "i": 1,
        "color": "yellow"
      },
      {
        "side": "b",
        "i": 3,
        "color": "blue"
      },
      {
        "side": "r",
        "i": 4,
        "color": "water"
      },
      {
        "side": "l",
        "i": 4,
        "color": "green"
      },
      {
        "side": "l",
        "i": 5,
        "color": "orange"
      },
      {
        "side": "t",
        "i": 5,
        "color": "purple"
      },
      {
        "side": "t",
        "i": 2,
        "color": "blue"
      }
    ],
    "tables": [
      [
        2,
        1
      ]
    ],
    "cars": [
      {
        "axis": "v",
        "r": 3,
        "c": 0,
        "color": "pink",
        "mud": 1
      },
      {
        "axis": "h",
        "r": 1,
        "c": 0,
        "color": "yellow"
      },
      {
        "axis": "v",
        "r": 0,
        "c": 3,
        "color": "blue"
      },
      {
        "axis": "h",
        "r": 4,
        "c": 2,
        "color": "green",
        "mud": 1
      },
      {
        "axis": "h",
        "r": 5,
        "c": 3,
        "color": "orange"
      },
      {
        "axis": "v",
        "r": 4,
        "c": 5,
        "color": "purple"
      },
      {
        "axis": "h",
        "r": 2,
        "c": 1,
        "color": "blue"
      }
    ]
  },
  // 17. Giờ cao điểm (màn lớn, thiết kế bằng tools/greedy.js + kiểm bằng bộ giải)
  {"name": "Giờ cao điểm", "cols": 7, "rows": 7, "splash": true, "gates": [{"side": "l", "i": 3, "color": "water"}, {"side": "b", "i": 5, "color": "water"}, {"side": "b", "i": 6, "color": "water"}, {"side": "b", "i": 1, "color": "water"}, {"side": "l", "i": 5, "color": "water"}, {"side": "t", "i": 1, "color": "pink"}, {"side": "r", "i": 1, "color": "blue"}, {"side": "t", "i": 4, "color": "yellow"}, {"side": "r", "i": 5, "color": "green"}, {"side": "b", "i": 2, "color": "orange"}, {"side": "l", "i": 6, "color": "purple"}, {"side": "r", "i": 3, "color": "pink"}, {"side": "t", "i": 6, "color": "blue"}, {"side": "b", "i": 0, "color": "green"}, {"side": "t", "i": 5, "color": "blue"}, {"side": "r", "i": 0, "color": "yellow"}, {"side": "r", "i": 6, "color": "orange"}, {"side": "t", "i": 3, "color": "purple"}], "cars": [{"axis": "h", "r": 1, "c": 2, "color": "blue", "mud": 0}, {"axis": "v", "r": 0, "c": 1, "color": "pink", "mud": 0}, {"axis": "v", "r": 2, "c": 1, "color": "pink", "mud": 1}, {"axis": "v", "r": 2, "c": 2, "color": "orange", "mud": 0}, {"axis": "v", "r": 1, "c": 4, "color": "yellow", "mud": 0}, {"axis": "h", "r": 3, "c": 5, "color": "pink", "mud": 1}, {"axis": "v", "r": 4, "c": 2, "color": "orange", "mud": 0}, {"axis": "h", "r": 5, "c": 3, "color": "green", "mud": 1}, {"axis": "v", "r": 4, "c": 5, "color": "blue", "mud": 0}, {"axis": "h", "r": 6, "c": 0, "color": "purple", "mud": 0}, {"axis": "v", "r": 4, "c": 0, "color": "green", "mud": 0}, {"axis": "v", "r": 0, "c": 6, "color": "blue", "mud": 1}, {"axis": "h", "r": 0, "c": 2, "color": "yellow", "mud": 0}, {"axis": "h", "r": 6, "c": 4, "color": "orange", "mud": 0}, {"axis": "v", "r": 2, "c": 3, "color": "purple", "mud": 0}], "tag": "Rất khó", "hint": "<b>Bãi lớn 15 xe.</b> Xe bùn lao vào xe sạch là văng bùn — dọn đường đúng thứ tự trước khi đưa xe bùn đi rửa."},
  // 18. Bàn xoay đôi (màn lớn, thiết kế bằng tools/greedy.js + kiểm bằng bộ giải)
  {"name": "Bàn xoay đôi", "cols": 7, "rows": 7, "splash": true, "tables": [[1, 1], [3, 4]], "paints": [{"r": 5, "c": 2, "color": "green"}, {"r": 0, "c": 4, "color": "purple"}], "barriers": [{"r": 3, "c": 0, "need": 2}], "gates": [{"side": "t", "i": 2, "color": "yellow"}, {"side": "r", "i": 3, "color": "blue"}, {"side": "l", "i": 1, "color": "pink"}, {"side": "b", "i": 4, "color": "orange"}, {"side": "l", "i": 5, "color": "water"}, {"side": "t", "i": 6, "color": "water"}, {"side": "r", "i": 5, "color": "green"}, {"side": "t", "i": 0, "color": "purple"}, {"side": "b", "i": 2, "color": "pink"}, {"side": "r", "i": 0, "color": "blue"}, {"side": "b", "i": 6, "color": "yellow"}, {"side": "l", "i": 3, "color": "orange"}], "cars": [{"axis": "h", "r": 1, "c": 1, "color": "yellow", "mud": 0}, {"axis": "v", "r": 3, "c": 4, "color": "blue", "mud": 0}, {"axis": "h", "r": 5, "c": 3, "color": "purple", "mud": 0}, {"axis": "v", "r": 4, "c": 6, "color": "yellow", "mud": 1}, {"axis": "v", "r": 1, "c": 0, "color": "purple", "mud": 0}, {"axis": "h", "r": 1, "c": 4, "color": "pink", "mud": 0}, {"axis": "v", "r": 4, "c": 1, "color": "pink", "mud": 0}, {"axis": "h", "r": 3, "c": 2, "color": "orange", "mud": 0}, {"axis": "v", "r": 4, "c": 0, "color": "purple", "mud": 0}], "tag": "Rất khó", "hint": "<b>Hai bàn xoay + vũng sơn.</b> Có xe phải xoay đúng lúc, có xe phải đổi màu mới ra được."},
  // 19. Tổng lực lớn (màn lớn, thiết kế bằng tools/greedy.js + kiểm bằng bộ giải)
  {"name": "Tổng lực lớn", "cols": 7, "rows": 8, "splash": true, "tables": [[3, 2]], "paints": [{"r": 6, "c": 5, "color": "pink"}], "muds": [[1, 4]], "sprays": [[5, 1]], "gates": [{"side": "t", "i": 0, "color": "water"}, {"side": "r", "i": 4, "color": "water"}, {"side": "b", "i": 3, "color": "water"}, {"side": "t", "i": 2, "color": "pink"}, {"side": "r", "i": 1, "color": "yellow"}, {"side": "b", "i": 0, "color": "blue"}, {"side": "l", "i": 4, "color": "green"}, {"side": "l", "i": 7, "color": "orange"}, {"side": "t", "i": 5, "color": "purple"}, {"side": "r", "i": 6, "color": "blue"}, {"side": "b", "i": 6, "color": "green"}, {"side": "l", "i": 2, "color": "yellow"}, {"side": "t", "i": 4, "color": "orange"}, {"side": "r", "i": 7, "color": "pink"}], "cars": [{"axis": "h", "r": 3, "c": 2, "color": "green", "mud": 2}, {"axis": "v", "r": 0, "c": 5, "color": "purple", "mud": 0}, {"axis": "h", "r": 2, "c": 4, "color": "yellow", "mud": 0}, {"axis": "h", "r": 2, "c": 2, "color": "yellow", "mud": 0}, {"axis": "v", "r": 5, "c": 0, "color": "blue", "mud": 0}, {"axis": "v", "r": 1, "c": 0, "color": "blue", "mud": 2}, {"axis": "v", "r": 4, "c": 5, "color": "purple", "mud": 0}, {"axis": "h", "r": 7, "c": 5, "color": "pink", "mud": 0}, {"axis": "v", "r": 2, "c": 6, "color": "green", "mud": 0}, {"axis": "v", "r": 5, "c": 2, "color": "pink", "mud": 0}, {"axis": "v", "r": 4, "c": 6, "color": "green", "mud": 0}, {"axis": "h", "r": 4, "c": 0, "color": "green", "mud": 0}, {"axis": "h", "r": 7, "c": 3, "color": "orange", "mud": 0}], "tag": "Rất khó", "hint": "<b>Bãi 7×8.</b> Bùn dày che cả dáng xe, vòi phun đẩy xe ngược lại — tính trước chỗ xe sẽ dừng."},
  // ===== v9: xe tải đẩy · ống cống dịch chuyển · barie liên động (màn 20–28, sinh bằng tools/mech.js) =====
  // 20. Xe tải húc
  {"name":"Xe tải húc","cols":5,"rows":5,"gates":[{"side":"r","i":2,"color":"blue"},{"side":"t","i":1,"color":"yellow"},{"side":"b","i":2,"color":"blue"},{"side":"b","i":4,"color":"yellow"},{"side":"l","i":0,"color":"yellow"}],"cars":[{"axis":"h","r":2,"c":1,"len":3,"color":"blue","mud":0,"truck":true},{"axis":"v","r":0,"c":3,"len":2,"color":"yellow","mud":0},{"axis":"v","r":3,"c":2,"len":2,"color":"blue","mud":0},{"axis":"v","r":3,"c":3,"len":2,"color":"yellow","mud":0},{"axis":"h","r":0,"c":1,"len":2,"color":"yellow","mud":0}],"tag":"Mới: xe tải","hint":"<b>Xe tải (sọc vàng đen, dài 3 ô).</b> Gặp xe khác thì <b>đẩy</b> nó đi 1 ô nếu phía sau còn chỗ, rồi dừng. Dùng để dọn đường cho xe bị kẹt.","tutorial":[{"say":"Chạm đầu TRÁI xe tải: lùi lại lấy đà.","focus":[{"car":0}],"do":["move",0,-1]},{"say":"Chạm đầu DƯỚI xe vàng: trượt xuống, sát xe tải.","focus":[{"car":1}],"do":["move",1,1]},{"say":"Chạm đầu PHẢI xe tải: húc xe vàng!","focus":[{"car":0},{"car":1}],"do":["move",0,1]},{"say":"Xe tải đẩy xe vàng đi 1 ô rồi dừng. Xe tải dài 3 ô, sọc vàng đen.","focus":[{"car":0},{"car":1}],"tap":true}]},
  // 21. Chui ống cống
  {"name":"Cổng trên tường","cols":5,"rows":5,"gates":[{"side":"r","i":0,"color":"blue"},{"side":"r","i":3,"color":"blue"},{"side":"r","i":2,"color":"blue"},{"side":"b","i":3,"color":"pink"}],"cars":[{"axis":"h","r":2,"c":1,"len":2,"color":"blue","mud":0},{"axis":"v","r":0,"c":4,"len":2,"color":"blue","mud":0},{"axis":"v","r":2,"c":4,"len":2,"color":"blue","mud":0},{"axis":"v","r":2,"c":3,"len":2,"color":"pink","mud":0}],"portals":[[[0,4,"N"],[3,0,"W"]]],"tag":"Mới: cổng dịch chuyển","hint":"<b>Cổng gắn trên tường, đi theo cặp cùng màu.</b> Xe chạy sát cổng thì chui vào, trồi ra ở cổng kia theo hướng vuông góc với tường đó, nên xe có thể đổi từ ngang sang dọc. Cần đủ chỗ trống cho cả thân xe.","tutorial":[{"say":"Chạm đầu TRÊN xe xanh: lao vào cổng trên tường.","focus":[{"car":1},{"face":[0,4,"N"]}],"do":["move",1,-1]},{"say":"Xe trồi ra ở cổng bên trái và đổi sang chạy NGANG.","focus":[{"car":1},{"face":[3,0,"W"]}],"tap":true}]},
  // 22. Một chốt, hai barie
  {"name":"Một chốt, hai barie","cols":5,"rows":5,"gates":[{"side":"t","i":0,"color":"blue"},{"side":"l","i":4,"color":"yellow"},{"side":"l","i":0,"color":"yellow"},{"side":"l","i":2,"color":"blue"},{"side":"l","i":1,"color":"blue"}],"cars":[{"axis":"h","r":2,"c":3,"len":2,"color":"blue","mud":0},{"axis":"h","r":4,"c":3,"len":2,"color":"yellow","mud":0},{"axis":"h","r":0,"c":2,"len":2,"color":"yellow","mud":0},{"axis":"v","r":2,"c":0,"len":2,"color":"blue","mud":0},{"axis":"h","r":1,"c":3,"len":2,"color":"blue","mud":0}],"switches":[{"r":0,"c":4,"a":[[3,1]],"b":[[4,2]]}],"tag":"Mới: barie liên động","hint":"<b>Chốt A|B.</b> Không xe nào đè chốt: barie A mở, barie B đóng. Có xe đứng trên chốt: A đóng, B mở.","tutorial":[{"say":"Chạm đầu PHẢI xe vàng: đưa nó lên chốt.","focus":[{"car":2},{"cell":[0,4]}],"do":["move",2,1]},{"say":"Xe đè chốt: barie A đóng, barie B mở. Nhả chốt thì ngược lại.","focus":[{"cell":[0,4]},{"cell":[3,1]},{"cell":[4,2]}],"tap":true},{"say":"Barie B đã mở. Chạm đầu TRÁI xe vàng dưới.","focus":[{"car":1},{"cell":[4,2]}],"do":["move",1,-1]}]},
  // 23. Chốt hai chiều
  {"name":"Chốt hai chiều","cols":6,"rows":6,"gates":[{"side":"l","i":1,"color":"yellow"},{"side":"b","i":4,"color":"blue"},{"side":"r","i":3,"color":"blue"},{"side":"b","i":1,"color":"pink"},{"side":"l","i":0,"color":"green"},{"side":"b","i":5,"color":"yellow"},{"side":"b","i":0,"color":"yellow"},{"side":"b","i":3,"color":"blue"}],"cars":[{"axis":"v","r":2,"c":5,"len":2,"color":"yellow","mud":0},{"axis":"v","r":3,"c":4,"len":2,"color":"blue","mud":0},{"axis":"v","r":4,"c":3,"len":2,"color":"blue","mud":0},{"axis":"v","r":4,"c":1,"len":2,"color":"pink","mud":0},{"axis":"h","r":0,"c":1,"len":2,"color":"green","mud":0},{"axis":"h","r":1,"c":3,"len":2,"color":"yellow","mud":0},{"axis":"v","r":3,"c":0,"len":2,"color":"yellow","mud":0},{"axis":"h","r":3,"c":2,"len":2,"color":"blue","mud":0}],"switches":[{"r":1,"c":5,"a":[[0,3]],"b":[[0,0]]}],"tag":"Khó vừa","hint":"<b>Barie liên động.</b> Xe đè chốt thì mở lối B nhưng đóng lối A — chọn xe nào đè chốt và đè lúc nào."},
  // 24. Húc qua ống
  {"name":"Húc qua cổng","cols":5,"rows":5,"gates":[{"side":"t","i":0,"color":"yellow"},{"side":"r","i":4,"color":"pink"},{"side":"l","i":1,"color":"blue"},{"side":"b","i":2,"color":"yellow"},{"side":"t","i":4,"color":"yellow"}],"cars":[{"axis":"v","r":2,"c":0,"len":3,"color":"yellow","mud":0,"truck":true},{"axis":"h","r":4,"c":2,"len":2,"color":"pink","mud":0},{"axis":"h","r":1,"c":3,"len":2,"color":"blue","mud":0},{"axis":"v","r":2,"c":3,"len":2,"color":"yellow","mud":0},{"axis":"v","r":3,"c":1,"len":2,"color":"yellow","mud":0}],"portals":[[[4,0,"S"],[2,4,"E"]]],"tag":"Xe tải + cổng","hint":"<b>Xe tải cũng chui được cổng.</b> Trồi ra rồi vẫn húc được xe trước mặt, theo hướng mới."},
  // 25. Bàn xoay và xe tải
  {"name":"Bàn xoay và xe tải","cols":6,"rows":6,"gates":[{"side":"l","i":0,"color":"green"},{"side":"t","i":1,"color":"blue"},{"side":"b","i":5,"color":"yellow"},{"side":"t","i":3,"color":"pink"},{"side":"t","i":2,"color":"yellow"},{"side":"r","i":5,"color":"blue"},{"side":"l","i":1,"color":"yellow"},{"side":"l","i":2,"color":"blue"}],"cars":[{"axis":"h","r":0,"c":1,"len":3,"color":"green","mud":0,"truck":true},{"axis":"v","r":2,"c":5,"len":2,"color":"blue","mud":0},{"axis":"v","r":0,"c":5,"len":2,"color":"yellow","mud":0},{"axis":"v","r":3,"c":3,"len":2,"color":"pink","mud":0},{"axis":"v","r":3,"c":2,"len":2,"color":"yellow","mud":0},{"axis":"h","r":5,"c":2,"len":2,"color":"blue","mud":0},{"axis":"h","r":1,"c":0,"len":2,"color":"yellow","mud":0},{"axis":"v","r":1,"c":2,"len":2,"color":"blue","mud":0}],"tables":[[4,4]],"tag":"Khó","hint":"<b>Xe tải + bàn xoay.</b> Xe tải đẩy cả hàng xe; bàn xoay đổi hướng xe để thoát bằng cổng khác — phối hợp hai thứ."},
  // 26. Đèn đỏ và chốt
  {"name":"Đèn đỏ và chốt","cols":6,"rows":6,"gates":[{"side":"r","i":0,"color":"blue"},{"side":"b","i":2,"color":"pink"},{"side":"l","i":2,"color":"yellow"},{"side":"t","i":4,"color":"green"},{"side":"b","i":3,"color":"blue"},{"side":"t","i":1,"color":"green"},{"side":"l","i":3,"color":"green"},{"side":"t","i":2,"color":"pink"}],"cars":[{"axis":"h","r":0,"c":3,"len":2,"color":"blue","mud":0},{"axis":"v","r":3,"c":2,"len":2,"color":"pink","mud":0},{"axis":"h","r":2,"c":4,"len":2,"color":"yellow","mud":0},{"axis":"v","r":4,"c":4,"len":2,"color":"green","mud":0},{"axis":"v","r":2,"c":3,"len":2,"color":"blue","mud":0},{"axis":"v","r":4,"c":1,"len":2,"color":"green","mud":0},{"axis":"h","r":3,"c":4,"len":2,"color":"green","mud":0},{"axis":"v","r":1,"c":2,"len":2,"color":"pink","mud":0}],"switches":[{"r":0,"c":1,"a":[[1,3]],"b":[[2,0]]}],"lights":[{"r":3,"c":0,"phase":1}],"tag":"Khó","hint":"<b>Đèn giao thông + chốt.</b> Đèn đổi xanh/đỏ sau mỗi lượt, chốt đổi barie theo xe đè — tính cả nhịp đèn lẫn vị trí xe."},
  // 27. Hàng xe tải
  {"name":"Hàng xe tải","cols":6,"rows":6,"gates":[{"side":"t","i":3,"color":"blue"},{"side":"r","i":0,"color":"pink"},{"side":"r","i":1,"color":"yellow"},{"side":"t","i":5,"color":"green"},{"side":"r","i":5,"color":"yellow"},{"side":"b","i":2,"color":"green"},{"side":"l","i":5,"color":"pink"},{"side":"b","i":1,"color":"pink"}],"cars":[{"axis":"v","r":2,"c":3,"len":3,"color":"blue","mud":0,"truck":true},{"axis":"h","r":1,"c":1,"len":2,"color":"pink","mud":0},{"axis":"h","r":4,"c":4,"len":2,"color":"yellow","mud":0},{"axis":"v","r":2,"c":5,"len":2,"color":"green","mud":0},{"axis":"h","r":5,"c":3,"len":2,"color":"yellow","mud":0},{"axis":"v","r":2,"c":2,"len":2,"color":"green","mud":0},{"axis":"h","r":4,"c":0,"len":2,"color":"pink","mud":0},{"axis":"h","r":0,"c":0,"len":2,"color":"pink","mud":0}],"tag":"Rất khó","hint":"<b>Một xe tải, nhiều xe kẹt.</b> Xe tải phải đẩy đúng xe, đúng chiều, đúng lúc — đẩy nhầm là chặn luôn lối thoát."},
  // 28. Tổng hợp xe tải
  {"name":"Tổng hợp xe tải","cols":5,"rows":5,"gates":[{"side":"l","i":4,"color":"yellow"},{"side":"t","i":2,"color":"yellow"},{"side":"b","i":0,"color":"blue"},{"side":"b","i":3,"color":"pink"}],"cars":[{"axis":"h","r":4,"c":0,"len":3,"color":"yellow","mud":0,"truck":true},{"axis":"v","r":2,"c":2,"len":2,"color":"yellow","mud":0},{"axis":"v","r":3,"c":4,"len":2,"color":"blue","mud":0},{"axis":"v","r":1,"c":3,"len":2,"color":"pink","mud":0},{"axis":"v","r":0,"c":4,"len":2,"color":"blue","mud":0},{"axis":"v","r":0,"c":1,"len":2,"color":"blue","mud":0}],"portals":[[[0,4,"N"],[0,1,"N"]]],"switches":[{"r":3,"c":3,"a":[[0,3]],"b":[[3,1]]}],"tag":"Rất khó","hint":"<b>Xe tải + cổng + chốt.</b> Ba cơ chế cùng lúc: đẩy xe vào chỗ cần, chui cổng sang bên kia, đè chốt để mở lối."},
  {"name":"Trụ xoay","cols":5,"rows":5,"gates":[{"side":"t","i":3,"color":"pink"},{"side":"r","i":3,"color":"pink"}],"cars":[{"axis":"v","r":3,"c":2,"len":2,"color":"pink","mud":0},{"axis":"h","r":3,"c":0,"len":2,"color":"pink","mud":0}],"pylons":[{"r":1,"c":2,"o":"h","to":[[4,3,"S"],[3,0,"W"]]}],"tag":"Mới: trụ xoay","hint":"<b>Trụ xoay có 2 mặt cổng.</b> Mỗi lần chạm trụ xoay thêm 90° (luôn cùng một chiều, tính 1 lượt): mặt cổng đổi sang hướng khác, xe nào vào được và ra hướng nào cũng đổi theo.","tutorial":[{"say":"Chạm vào TRỤ: nó xoay 90°.","focus":[{"pylon":0}],"do":["pyl",0]},{"say":"Chạm thêm lần nữa: mỗi lần xoay 90° cùng chiều.","focus":[{"pylon":0}],"do":["pyl",0]},{"say":"Mặt cổng giờ quay xuống. Chạm đầu TRÊN xe hồng đứng.","focus":[{"car":0},{"face":[2,2,"N"]}],"do":["move",0,-1]},{"say":"Xe đã chui qua mặt trụ. Giờ chạm đầu PHẢI xe hồng nằm để nó chạy ra.","focus":[{"car":1}],"do":["move",1,1]}]},
  {"name":"Hai mặt cổng","cols":5,"rows":5,"gates":[{"side":"t","i":3,"color":"yellow"},{"side":"l","i":0,"color":"yellow"},{"side":"b","i":0,"color":"blue"},{"side":"b","i":2,"color":"blue"},{"side":"r","i":2,"color":"pink"}],"cars":[{"axis":"h","r":3,"c":1,"len":2,"color":"yellow","mud":0},{"axis":"v","r":2,"c":0,"len":2,"color":"yellow","mud":0},{"axis":"h","r":4,"c":0,"len":2,"color":"blue","mud":0},{"axis":"v","r":0,"c":0,"len":2,"color":"blue","mud":0},{"axis":"h","r":2,"c":3,"len":2,"color":"pink","mud":0}],"portals":[[[3,0,"W"],[4,0,"W"]]],"pylons":[{"r":3,"c":3,"o":"v","to":[[0,0,"N"],[4,1,"S"]]}],"tag":"Khó","hint":"<b>Trụ + cổng tường.</b> Mỗi mặt trụ nối với một cổng ngoài tường. Xoay trụ đúng lúc để xe đi đúng đường."},
  {"name":"Trụ và xe tải","cols":5,"rows":5,"gates":[{"side":"l","i":4,"color":"yellow"},{"side":"r","i":1,"color":"pink"},{"side":"r","i":0,"color":"pink"},{"side":"l","i":3,"color":"yellow"},{"side":"b","i":0,"color":"pink"}],"cars":[{"axis":"h","r":4,"c":2,"len":3,"color":"yellow","mud":0,"truck":true},{"axis":"v","r":3,"c":1,"len":2,"color":"pink","mud":0},{"axis":"h","r":3,"c":3,"len":2,"color":"pink","mud":0},{"axis":"h","r":2,"c":2,"len":2,"color":"yellow","mud":0},{"axis":"v","r":0,"c":1,"len":2,"color":"pink","mud":0}],"pylons":[{"r":2,"c":1,"o":"h","to":[[2,4,"E"],[3,4,"E"]]}],"tag":"Rất khó","hint":"<b>Trụ xoay + xe tải + cổng.</b> Xoay trụ để mở đúng cổng, rồi húc xe vào chỗ cần."},
];
if (typeof module !== 'undefined') module.exports = CW_LEVELS;
