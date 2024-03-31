use crate::fs::common::*;

/// Serializes specified diced sprites to JSON string.
pub(crate) fn sprites_to_json(sprites: &[DicedSprite]) -> String {
    let sprites = sprites
        .iter()
        .map(sprite_to_json)
        .collect::<Vec<_>>()
        .join(",");

    format!("[{sprites}\n]\n")
}

fn sprite_to_json(sprite: &DicedSprite) -> String {
    let id = &sprite.id;
    let atlas = sprite.atlas_index;
    let vertices = sprite
        .vertices
        .iter()
        .map(|v| format!(r#"{{ "x": {}, "y": {} }}"#, v.x, v.y))
        .collect::<Vec<_>>()
        .join(", ");
    let uvs = sprite
        .uvs
        .iter()
        .map(|uv| format!(r#"{{ "u": {}, "v": {} }}"#, uv.u, uv.v))
        .collect::<Vec<_>>()
        .join(", ");
    let indices = sprite
        .indices
        .iter()
        .map(|i| i.to_string())
        .collect::<Vec<_>>()
        .join(", ");
    let x = sprite.rect.x;
    let y = sprite.rect.y;
    let width = sprite.rect.width;
    let height = sprite.rect.height;

    format!(
        r#"
    {{
        "id": "{id}",
        "atlas": {atlas},
        "vertices": [{vertices}],
        "uvs": [{uvs}],
        "indices": [{indices}],
        "rect": {{ "x": {x}, "y": {y}, "width": {width}, "height": {height} }}
    }}"#
    )
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn builds_sprites_json() {
        let sprites = [
            DicedSprite {
                id: "foo/bar/img".to_owned(),
                atlas_index: 0,
                vertices: vec![Vertex::new(1.0, -2.0), Vertex::new(-3.0, 4.525)],
                uvs: vec![Uv::new(0.1, 0.2), Uv::new(0.3, 0.4)],
                indices: vec![1, 2, 3],
                rect: Rect::new(0.5, 0.5, 100.0, 50.0),
                pivot: Pivot::new(0.0, 0.0),
            },
            DicedSprite {
                id: "img".to_owned(),
                atlas_index: 1,
                vertices: vec![Vertex::new(-1.0, 2.0)],
                uvs: vec![Uv::new(0.01, 0.02)],
                indices: vec![0],
                rect: Rect::new(-1.5, 0.0, 0.0, 10.10),
                pivot: Pivot::new(0.0, 0.0),
            },
        ];
        assert_eq!(
            sprites_to_json(&sprites),
            r#"[
    {
        "id": "foo/bar/img",
        "atlas": 0,
        "vertices": [{ "x": 1, "y": -2 }, { "x": -3, "y": 4.525 }],
        "uvs": [{ "u": 0.1, "v": 0.2 }, { "u": 0.3, "v": 0.4 }],
        "indices": [1, 2, 3],
        "rect": { "x": 0.5, "y": 0.5, "width": 100, "height": 50 }
    },
    {
        "id": "img",
        "atlas": 1,
        "vertices": [{ "x": -1, "y": 2 }],
        "uvs": [{ "u": 0.01, "v": 0.02 }],
        "indices": [0],
        "rect": { "x": -1.5, "y": 0, "width": 0, "height": 10.1 }
    }
]
"#
        );
    }
}
